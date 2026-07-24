using IncidentRca.Domain.Enums;

namespace IncidentRca.Application.Models;

public sealed class IncidentToolResult
{
    public int IncidentId { get; set; }

    public string IncidentNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IntegrationName { get; set; } = string.Empty;

    public string AffectedSystem { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string BusinessImpact { get; set; } = string.Empty;

    public string TechnicalEvidence { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? CorrelationId { get; set; }

    public IncidentSeverity Severity { get; set; }

    public IncidentStatus Status { get; set; }

    public RootCauseCategory KnownRootCauseCategory { get; set; }

    public string? ConfirmedResolution { get; set; }

    public DateTimeOffset DetectedAtUtc { get; set; }
}

public sealed class SimilarIncidentResult
{
    public string IncidentNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public RootCauseCategory RootCauseCategory { get; set; }

    public string Resolution { get; set; } = string.Empty;

    public int SimilarityScore { get; set; }

    public List<string> MatchingSignals { get; set; } = [];
}

public sealed class RunbookToolResult
{
    public string RunbookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string IntegrationName { get; set; } = string.Empty;

    public RootCauseCategory Category { get; set; }

    public string Symptoms { get; set; } = string.Empty;

    public List<string> InvestigationSteps { get; set; } = [];

    public List<string> CorrectiveActions { get; set; } = [];

    public List<string> PreventiveActions { get; set; } = [];

    public List<string> ValidationSteps { get; set; } = [];

    public string EscalationTeam { get; set; } = string.Empty;
}

public sealed class EvidenceValidationToolResult
{
    public List<string> ConfirmedFacts { get; set; } = [];

    public List<string> Assumptions { get; set; } = [];

    public List<string> MissingInformation { get; set; } = [];

    public bool HasSufficientEvidence { get; set; }

    public string ValidationSummary { get; set; } = string.Empty;
}
