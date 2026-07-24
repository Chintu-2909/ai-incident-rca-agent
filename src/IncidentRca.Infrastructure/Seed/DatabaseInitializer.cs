using System.Data;
using IncidentRca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IncidentRca.Infrastructure.Seed;

public sealed class DatabaseInitializer(
    IncidentRcaDbContext dbContext,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await EnsureIncidentClosureSchemaAsync(
            cancellationToken);

        if (!await dbContext.Incidents.AnyAsync(cancellationToken))
        {
            var incidents = SyntheticDataFactory.CreateIncidents();

            await dbContext.Incidents.AddRangeAsync(
                incidents,
                cancellationToken);

            logger.LogInformation(
                "Prepared {IncidentCount} synthetic incidents",
                incidents.Count);
        }

        var seedRunbooks =
            SyntheticDataFactory.CreateRunbooks();

        var existingRunbookCodeList =
            await dbContext.Runbooks
                .AsNoTracking()
                .Select(runbook => runbook.RunbookCode)
                .ToListAsync(cancellationToken);

        var existingRunbookCodes =
            existingRunbookCodeList.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var missingRunbooks = seedRunbooks
            .Where(runbook =>
                !existingRunbookCodes.Contains(
                    runbook.RunbookCode))
            .ToList();

        if (missingRunbooks.Count > 0)
        {
            await dbContext.Runbooks.AddRangeAsync(
                missingRunbooks,
                cancellationToken);

            logger.LogInformation(
                "Prepared {RunbookCount} missing synthetic runbooks",
                missingRunbooks.Count);
        }
        else
        {
            logger.LogInformation(
                "All synthetic runbooks are already available");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Incident RCA database initialization completed");
    }

    private async Task EnsureIncidentClosureSchemaAsync(
        CancellationToken cancellationToken)
    {
        var connection =
            dbContext.Database.GetDbConnection();

        var shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var existingColumns =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            await using (var command =
                connection.CreateCommand())
            {
                command.CommandText =
                    "PRAGMA table_info('Incidents');";

                await using var reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken);

                while (await reader.ReadAsync(
                    cancellationToken))
                {
                    existingColumns.Add(
                        reader.GetString(1));
                }
            }

            var requiredColumns =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["StatusUpdatedAtUtc"] =
                        "TEXT NULL",
                    ["ClosedBy"] =
                        "TEXT NULL",
                    ["ClosedAtUtc"] =
                        "TEXT NULL",
                    ["ClosureSummary"] =
                        "TEXT NULL",
                    ["FinalRcaInvestigationId"] =
                        "TEXT NULL",
                    ["ClosureValidationConfirmed"] =
                        "INTEGER NOT NULL DEFAULT 0"
                };

            foreach (var column in requiredColumns)
            {
                if (existingColumns.Contains(column.Key))
                {
                    continue;
                }

                await using var alterCommand =
                    connection.CreateCommand();

                alterCommand.CommandText =
                    $"ALTER TABLE Incidents " +
                    $"ADD COLUMN {column.Key} " +
                    $"{column.Value};";

                await alterCommand.ExecuteNonQueryAsync(
                    cancellationToken);

                logger.LogInformation(
                    "Added missing incident column {ColumnName}",
                    column.Key);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
