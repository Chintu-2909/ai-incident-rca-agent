using IncidentRca.Domain.Enums;

namespace IncidentRca.Domain.Models;

public sealed record StartInvestigationRequest(
    string IncidentNumber,
    string Objective);

public sealed class IncidentInvestigationResult
{
    public Guid InvestigationId { get; set; }

    public string IncidentNumber { get; set; } = string.Empty;

    public string Classification { get; set; } = string.Empty;

    public RootCauseCategory RootCauseCategory { get; set; }
        = RootCauseCategory.Unknown;

    public ConfidenceLevel Confidence { get; set; }
        = ConfidenceLevel.InsufficientEvidence;

    public string ProbableRootCause { get; set; } = string.Empty;

    public List<string> ConfirmedFacts { get; set; } = [];

    public List<string> Assumptions { get; set; } = [];

    public List<string> MissingInformation { get; set; } = [];

    public List<string> Evidence { get; set; } = [];

    public string TechnicalSummary { get; set; } = string.Empty;

    public string ServiceNowWorkNote { get; set; } = string.Empty;

    public string StakeholderUpdate { get; set; } = string.Empty;

    public string RootCauseAnalysis { get; set; } = string.Empty;

    public List<string> CorrectiveActions { get; set; } = [];

    public List<string> PreventiveActions { get; set; } = [];

    public List<string> ValidationChecklist { get; set; } = [];

    public string ShiftHandoverNote { get; set; } = string.Empty;

    public List<AgentToolStep> ToolsExecuted { get; set; } = [];

    public DateTimeOffset GeneratedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;
}

public sealed class AgentToolStep
{
    public int StepNumber { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public bool Successful { get; set; }
}

public sealed class EvidenceValidationResult
{
    public List<string> ConfirmedFacts { get; set; } = [];

    public List<string> Assumptions { get; set; } = [];

    public List<string> MissingInformation { get; set; } = [];

    public bool HasSufficientEvidence { get; set; }

    public string ValidationSummary { get; set; } = string.Empty;
}
