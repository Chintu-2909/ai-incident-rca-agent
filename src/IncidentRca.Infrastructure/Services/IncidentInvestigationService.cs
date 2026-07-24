using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using IncidentRca.Application.Abstractions;
using IncidentRca.Application.Models;
using IncidentRca.Domain.Entities;
using IncidentRca.Domain.Enums;
using IncidentRca.Domain.Models;
using IncidentRca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IncidentRca.Infrastructure.Services;

public sealed class IncidentInvestigationService(
    IncidentRcaDbContext dbContext,
    IIncidentAgentTools agentTools,
    IAiAgentClient aiAgentClient,
    ILogger<IncidentInvestigationService> logger)
    : IIncidentInvestigationService
{
    private static readonly JsonSerializerOptions JsonOptions =
        CreateJsonOptions();

    public async Task<IncidentInvestigationResult> InvestigateAsync(
        StartInvestigationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.IncidentNumber))
        {
            throw new ArgumentException(
                "Incident number is required.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Objective))
        {
            throw new ArgumentException(
                "Investigation objective is required.",
                nameof(request));
        }

        var incidentNumber = request.IncidentNumber
            .Trim()
            .ToUpperInvariant();

        var objective = request.Objective.Trim();

        if (objective.Length > 2000)
        {
            throw new ArgumentException(
                "Investigation objective cannot exceed 2000 characters.",
                nameof(request));
        }

        logger.LogInformation(
            "Starting investigation for {IncidentNumber}",
            incidentNumber);

        var incident = await agentTools.GetIncidentDetailsAsync(
            incidentNumber,
            cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException(
                $"Incident {incidentNumber} was not found.");
        }

        if (incident.Status ==
            IncidentStatus.Closed)
        {
            throw new InvalidOperationException(
                $"Incident {incidentNumber} is closed and read-only. " +
                "Review the approved final RCA through " +
                "Investigation History.");
        }

        var investigation = new Investigation
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.IncidentId,
            Objective = objective,
            AiProvider = aiAgentClient.ProviderName,
            ModelName = aiAgentClient.ModelName,
            Status = InvestigationStatus.InProgress,
            RootCauseCategory =
                incident.KnownRootCauseCategory,
            Confidence =
                ConfidenceLevel.InsufficientEvidence,
            ResultJson = "{}",
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Investigations.Add(investigation);

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var toolSteps = new List<AgentToolStep>();

            await RecordToolExecutionAsync(
                investigation,
                stepNumber: 1,
                toolName: "GetIncidentDetails",
                arguments: new
                {
                    incidentNumber
                },
                result: incident,
                summary:
                    $"Retrieved incident {incident.IncidentNumber}.",
                toolSteps,
                cancellationToken);

            var similarIncidents =
                await ExecuteAndRecordAsync(
                    investigation,
                    stepNumber: 2,
                    toolName: "FindSimilarIncidents",
                    arguments: new
                    {
                        incidentNumber
                    },
                    operation: () =>
                        agentTools.FindSimilarIncidentsAsync(
                            incidentNumber,
                            cancellationToken),
                    summaryFactory: result =>
                        $"Found {result.Count} related incidents.",
                    toolSteps,
                    cancellationToken);

            var runbook =
                await ExecuteAndRecordAsync(
                    investigation,
                    stepNumber: 3,
                    toolName: "GetIntegrationRunbook",
                    arguments: new
                    {
                        incident.IntegrationName,
                        category =
                            incident.KnownRootCauseCategory
                    },
                    operation: () =>
                        agentTools.GetIntegrationRunbookAsync(
                            incident.IntegrationName,
                            incident.KnownRootCauseCategory,
                            cancellationToken),
                    summaryFactory: result =>
                        result is null
                            ? "No matching runbook was found."
                            : $"Retrieved runbook {result.RunbookCode}.",
                    toolSteps,
                    cancellationToken);

            var evidenceValidation =
                await ExecuteAndRecordAsync(
                    investigation,
                    stepNumber: 4,
                    toolName: "ValidateIncidentEvidence",
                    arguments: new
                    {
                        incidentNumber =
                            incident.IncidentNumber
                    },
                    operation: () =>
                        agentTools.ValidateIncidentEvidenceAsync(
                            incident,
                            cancellationToken),
                    summaryFactory: result =>
                        result.ValidationSummary,
                    toolSteps,
                    cancellationToken);

            var aiContext = new AiIncidentContext
            {
                InvestigationId = investigation.Id,
                Objective = objective,
                Incident = incident,
                SimilarIncidents = similarIncidents,
                Runbook = runbook,
                EvidenceValidation = evidenceValidation
            };

            var report = await aiAgentClient.GenerateReportAsync(
                aiContext,
                cancellationToken);

            report.ToolsExecuted = toolSteps;

            investigation.Status =
                InvestigationStatus.Completed;

            investigation.RootCauseCategory =
                report.RootCauseCategory;

            investigation.Confidence =
                report.Confidence;

            investigation.ResultJson =
                JsonSerializer.Serialize(
                    report,
                    JsonOptions);

            investigation.CompletedAtUtc =
                DateTimeOffset.UtcNow;

            investigation.FailureReason = null;

            await dbContext.SaveChangesAsync(
                cancellationToken);

            logger.LogInformation(
                "Completed investigation {InvestigationId} for {IncidentNumber}",
                investigation.Id,
                incidentNumber);

            return report;
        }
        catch (Exception exception)
        {
            investigation.Status =
                InvestigationStatus.Failed;

            investigation.FailureReason =
                exception.Message.Length > 4000
                    ? exception.Message[..4000]
                    : exception.Message;

            investigation.CompletedAtUtc =
                DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(
                CancellationToken.None);

            logger.LogError(
                exception,
                "Investigation {InvestigationId} failed for {IncidentNumber}",
                investigation.Id,
                incidentNumber);

            throw;
        }
    }

    public async Task<IncidentInvestigationResult?>
        GetInvestigationAsync(
            Guid investigationId,
            CancellationToken cancellationToken = default)
    {
        var investigation = await dbContext.Investigations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == investigationId,
                cancellationToken);

        if (investigation is null ||
            investigation.Status !=
            InvestigationStatus.Completed ||
            string.IsNullOrWhiteSpace(
                investigation.ResultJson) ||
            investigation.ResultJson == "{}")
        {
            return null;
        }

        return JsonSerializer
            .Deserialize<IncidentInvestigationResult>(
                investigation.ResultJson,
                JsonOptions);
    }

    private async Task<T> ExecuteAndRecordAsync<T>(
        Investigation investigation,
        int stepNumber,
        string toolName,
        object arguments,
        Func<Task<T>> operation,
        Func<T, string> summaryFactory,
        ICollection<AgentToolStep> toolSteps,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var toolExecution = new ToolExecution
        {
            Id = Guid.NewGuid(),
            InvestigationId = investigation.Id,
            StepNumber = stepNumber,
            ToolName = toolName,
            ArgumentsJson = JsonSerializer.Serialize(
                arguments,
                JsonOptions),
            Status = ToolExecutionStatus.Started,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ToolExecutions.Add(toolExecution);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            var result = await operation();

            stopwatch.Stop();

            var summary = summaryFactory(result);

            toolExecution.ResultJson =
                JsonSerializer.Serialize(
                    result,
                    JsonOptions);

            toolExecution.Status =
                ToolExecutionStatus.Completed;

            toolExecution.CompletedAtUtc =
                DateTimeOffset.UtcNow;

            toolExecution.DurationMilliseconds =
                stopwatch.ElapsedMilliseconds;

            toolSteps.Add(new AgentToolStep
            {
                StepNumber = stepNumber,
                ToolName = toolName,
                Summary = summary,
                Successful = true
            });

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return result;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            toolExecution.Status =
                ToolExecutionStatus.Failed;

            toolExecution.ErrorMessage =
                exception.Message;

            toolExecution.CompletedAtUtc =
                DateTimeOffset.UtcNow;

            toolExecution.DurationMilliseconds =
                stopwatch.ElapsedMilliseconds;

            toolSteps.Add(new AgentToolStep
            {
                StepNumber = stepNumber,
                ToolName = toolName,
                Summary = exception.Message,
                Successful = false
            });

            await dbContext.SaveChangesAsync(
                CancellationToken.None);

            throw;
        }
    }

    private async Task RecordToolExecutionAsync<T>(
        Investigation investigation,
        int stepNumber,
        string toolName,
        object arguments,
        T result,
        string summary,
        ICollection<AgentToolStep> toolSteps,
        CancellationToken cancellationToken)
    {
        var toolExecution = new ToolExecution
        {
            Id = Guid.NewGuid(),
            InvestigationId = investigation.Id,
            StepNumber = stepNumber,
            ToolName = toolName,
            ArgumentsJson = JsonSerializer.Serialize(
                arguments,
                JsonOptions),
            ResultJson = JsonSerializer.Serialize(
                result,
                JsonOptions),
            Status = ToolExecutionStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            DurationMilliseconds = 0
        };

        dbContext.ToolExecutions.Add(toolExecution);

        toolSteps.Add(new AgentToolStep
        {
            StepNumber = stepNumber,
            ToolName = toolName,
            Summary = summary,
            Successful = true
        });

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }
}
