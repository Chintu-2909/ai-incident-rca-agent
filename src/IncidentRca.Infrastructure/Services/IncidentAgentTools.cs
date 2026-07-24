using IncidentRca.Application.Abstractions;
using IncidentRca.Application.Models;
using IncidentRca.Domain.Entities;
using IncidentRca.Domain.Enums;
using IncidentRca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IncidentRca.Infrastructure.Services;

public sealed class IncidentAgentTools(
    IncidentRcaDbContext dbContext,
    ILogger<IncidentAgentTools> logger)
    : IIncidentAgentTools
{
    public async Task<IncidentToolResult?> GetIncidentDetailsAsync(
        string incidentNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(incidentNumber);

        var normalizedIncidentNumber = incidentNumber
            .Trim()
            .ToUpperInvariant();

        logger.LogInformation(
            "Executing GetIncidentDetails for {IncidentNumber}",
            normalizedIncidentNumber);

        var incident = await dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IncidentNumber == normalizedIncidentNumber,
                cancellationToken);

        return incident is null
            ? null
            : MapIncident(incident);
    }

    public async Task<IReadOnlyList<SimilarIncidentResult>>
        FindSimilarIncidentsAsync(
            string incidentNumber,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(incidentNumber);

        var normalizedIncidentNumber = incidentNumber
            .Trim()
            .ToUpperInvariant();

        logger.LogInformation(
            "Executing FindSimilarIncidents for {IncidentNumber}",
            normalizedIncidentNumber);

        var sourceIncident = await dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IncidentNumber == normalizedIncidentNumber,
                cancellationToken);

        if (sourceIncident is null)
        {
            return [];
        }

        var candidates = await dbContext.Incidents
            .AsNoTracking()
            .Where(x => x.IncidentNumber != normalizedIncidentNumber)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(candidate =>
                CalculateSimilarity(sourceIncident, candidate))
            .Where(result => result.SimilarityScore > 0)
            .OrderByDescending(result => result.SimilarityScore)
            .ThenBy(result => result.IncidentNumber)
            .Take(3)
            .ToList();
    }

    public async Task<RunbookToolResult?>
        GetIntegrationRunbookAsync(
            string integrationName,
            RootCauseCategory category,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(integrationName);

        var normalizedIntegrationName = integrationName.Trim();

        logger.LogInformation(
            "Executing GetIntegrationRunbook for {IntegrationName} and {Category}",
            normalizedIntegrationName,
            category);

        var runbook = await dbContext.Runbooks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IntegrationName == normalizedIntegrationName
                     && x.Category == category,
                cancellationToken);

        return runbook is null
            ? null
            : MapRunbook(runbook);
    }

    public Task<EvidenceValidationToolResult>
        ValidateIncidentEvidenceAsync(
            IncidentToolResult incident,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Executing ValidateIncidentEvidence for {IncidentNumber}",
            incident.IncidentNumber);

        var confirmedFacts = new List<string>();
        var assumptions = new List<string>();
        var missingInformation = new List<string>();

        AddFact(
            confirmedFacts,
            !string.IsNullOrWhiteSpace(incident.ErrorCode),
            $"The recorded error code is {incident.ErrorCode}.");

        AddFact(
            confirmedFacts,
            !string.IsNullOrWhiteSpace(incident.ErrorMessage),
            $"The recorded error message is: {incident.ErrorMessage}");

        AddFact(
            confirmedFacts,
            !string.IsNullOrWhiteSpace(incident.CorrelationId),
            $"The incident has correlation ID {incident.CorrelationId}.");

        AddFact(
            confirmedFacts,
            !string.IsNullOrWhiteSpace(incident.BusinessImpact),
            $"Business impact: {incident.BusinessImpact}");

        AddFact(
            confirmedFacts,
            !string.IsNullOrWhiteSpace(incident.TechnicalEvidence),
            "Technical evidence was captured for the incident.");

        AddMissing(
            missingInformation,
            string.IsNullOrWhiteSpace(incident.ErrorCode),
            "A structured error code is missing.");

        AddMissing(
            missingInformation,
            string.IsNullOrWhiteSpace(incident.CorrelationId),
            "A correlation ID is missing.");

        AddMissing(
            missingInformation,
            string.IsNullOrWhiteSpace(incident.TechnicalEvidence),
            "Technical investigation evidence is missing.");

        AddMissing(
            missingInformation,
            string.IsNullOrWhiteSpace(incident.BusinessImpact),
            "The business impact is missing.");

        if (incident.KnownRootCauseCategory == RootCauseCategory.Unknown)
        {
            assumptions.Add(
                "The root-cause category has not been confirmed.");
        }
        else if (string.IsNullOrWhiteSpace(incident.ConfirmedResolution))
        {
            assumptions.Add(
                "The root-cause category is indicated, but no confirmed resolution has been recorded.");
        }
        else
        {
            confirmedFacts.Add(
                $"The recorded root-cause category is {incident.KnownRootCauseCategory}.");

            confirmedFacts.Add(
                $"The recorded resolution is: {incident.ConfirmedResolution}");
        }

        var hasSufficientEvidence =
            confirmedFacts.Count >= 4 &&
            !string.IsNullOrWhiteSpace(incident.ErrorCode) &&
            !string.IsNullOrWhiteSpace(incident.TechnicalEvidence);

        var result = new EvidenceValidationToolResult
        {
            ConfirmedFacts = confirmedFacts,
            Assumptions = assumptions,
            MissingInformation = missingInformation,
            HasSufficientEvidence = hasSufficientEvidence,
            ValidationSummary = hasSufficientEvidence
                ? "The incident contains sufficient structured evidence for an RCA draft."
                : "Additional evidence is required before presenting a confident RCA."
        };

        return Task.FromResult(result);
    }

    private static IncidentToolResult MapIncident(Incident incident)
    {
        return new IncidentToolResult
        {
            IncidentId = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Description = incident.Description,
            IntegrationName = incident.IntegrationName,
            AffectedSystem = incident.AffectedSystem,
            Environment = incident.Environment,
            BusinessImpact = incident.BusinessImpact,
            TechnicalEvidence = incident.TechnicalEvidence,
            ErrorCode = incident.ErrorCode,
            ErrorMessage = incident.ErrorMessage,
            CorrelationId = incident.CorrelationId,
            Severity = incident.Severity,
            Status = incident.Status,
            KnownRootCauseCategory =
                incident.KnownRootCauseCategory,
            ConfirmedResolution = incident.ConfirmedResolution,
            DetectedAtUtc = incident.DetectedAtUtc
        };
    }

    private static SimilarIncidentResult CalculateSimilarity(
        Incident source,
        Incident candidate)
    {
        var score = 0;
        var matchingSignals = new List<string>();

        if (source.IntegrationName == candidate.IntegrationName)
        {
            score += 20;
            matchingSignals.Add("Same integration");
        }

        if (source.AffectedSystem == candidate.AffectedSystem)
        {
            score += 15;
            matchingSignals.Add("Same affected system");
        }

        if (source.KnownRootCauseCategory ==
            candidate.KnownRootCauseCategory)
        {
            score += 40;
            matchingSignals.Add("Same root-cause category");
        }

        if (!string.IsNullOrWhiteSpace(source.ErrorCode) &&
            source.ErrorCode == candidate.ErrorCode)
        {
            score += 25;
            matchingSignals.Add("Same error code");
        }

        return new SimilarIncidentResult
        {
            IncidentNumber = candidate.IncidentNumber,
            Title = candidate.Title,
            ErrorCode = candidate.ErrorCode ?? string.Empty,
            RootCauseCategory =
                candidate.KnownRootCauseCategory,
            Resolution =
                candidate.ConfirmedResolution ?? string.Empty,
            SimilarityScore = score,
            MatchingSignals = matchingSignals
        };
    }

    private static RunbookToolResult MapRunbook(Runbook runbook)
    {
        return new RunbookToolResult
        {
            RunbookCode = runbook.RunbookCode,
            Title = runbook.Title,
            IntegrationName = runbook.IntegrationName,
            Category = runbook.Category,
            Symptoms = runbook.Symptoms,
            InvestigationSteps =
                SplitLines(runbook.InvestigationSteps),
            CorrectiveActions =
                SplitLines(runbook.CorrectiveActions),
            PreventiveActions =
                SplitLines(runbook.PreventiveActions),
            ValidationSteps =
                SplitLines(runbook.ValidationSteps),
            EscalationTeam = runbook.EscalationTeam
        };
    }

    private static List<string> SplitLines(string value)
    {
        return value
            .Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(RemoveNumberPrefix)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string RemoveNumberPrefix(string value)
    {
        var separatorIndex = value.IndexOf(". ", StringComparison.Ordinal);

        if (separatorIndex > 0 &&
            value[..separatorIndex].All(char.IsDigit))
        {
            return value[(separatorIndex + 2)..];
        }

        return value;
    }

    private static void AddFact(
        ICollection<string> collection,
        bool condition,
        string value)
    {
        if (condition)
        {
            collection.Add(value);
        }
    }

    private static void AddMissing(
        ICollection<string> collection,
        bool condition,
        string value)
    {
        if (condition)
        {
            collection.Add(value);
        }
    }
}
