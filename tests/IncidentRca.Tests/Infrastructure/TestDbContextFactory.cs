using IncidentRca.Infrastructure.Persistence;
using IncidentRca.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace IncidentRca.Tests.Infrastructure;

internal static class TestDbContextFactory
{
    public static async Task<IncidentRcaDbContext> CreateSeededAsync()
    {
        var options =
            new DbContextOptionsBuilder<IncidentRcaDbContext>()
                .UseInMemoryDatabase(
                    $"IncidentRcaTests-{Guid.NewGuid()}")
                .EnableSensitiveDataLogging()
                .Options;

        var dbContext = new IncidentRcaDbContext(options);

        await dbContext.Incidents.AddRangeAsync(
            SyntheticDataFactory.CreateIncidents());

        await dbContext.Runbooks.AddRangeAsync(
            SyntheticDataFactory.CreateRunbooks());

        await dbContext.SaveChangesAsync();

        return dbContext;
    }
}
