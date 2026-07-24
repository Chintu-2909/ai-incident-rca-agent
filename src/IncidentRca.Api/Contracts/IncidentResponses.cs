using IncidentRca.Domain.Entities;
using IncidentRca.Domain.Enums;

namespace IncidentRca.Api.Contracts;

public sealed record IncidentSummaryResponse(
    int Id,
    string IncidentNumber,
    string Title,
    string IntegrationName,
    string AffectedSystem,
    string Environment,
    IncidentSeverity Severity,
    IncidentStatus Status,
    RootCauseCategory KnownRootCauseCategory,
    DateTimeOffset DetectedAtUtc)
{
    public static IncidentSummaryResponse FromEntity(Incident incident)
    {
        return new IncidentSummaryResponse(
            incident.Id,
            incident.IncidentNumber,
            incident.Title,
            incident.IntegrationName,
            incident.AffectedSystem,
            incident.Environment,
            incident.Severity,
            incident.Status,
            incident.KnownRootCauseCategory,
            incident.DetectedAtUtc);
    }
}

public sealed record IncidentDetailsResponse(
    int Id,
    string IncidentNumber,
    string Title,
    string Description,
    string IntegrationName,
    string AffectedSystem,
    string Environment,
    string BusinessImpact,
    string TechnicalEvidence,
    string? ErrorCode,
    string? ErrorMessage,
    string? CorrelationId,
    IncidentSeverity Severity,
    IncidentStatus Status,
    RootCauseCategory KnownRootCauseCategory,
    string? ConfirmedResolution,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? StatusUpdatedAtUtc,
    string? ClosedBy,
    DateTimeOffset? ClosedAtUtc,
    string? ClosureSummary,
    Guid? FinalRcaInvestigationId,
    bool ClosureValidationConfirmed)
{
    public static IncidentDetailsResponse FromEntity(Incident incident)
    {
        return new IncidentDetailsResponse(
            incident.Id,
            incident.IncidentNumber,
            incident.Title,
            incident.Description,
            incident.IntegrationName,
            incident.AffectedSystem,
            incident.Environment,
            incident.BusinessImpact,
            incident.TechnicalEvidence,
            incident.ErrorCode,
            incident.ErrorMessage,
            incident.CorrelationId,
            incident.Severity,
            incident.Status,
            incident.KnownRootCauseCategory,
            incident.ConfirmedResolution,
            incident.DetectedAtUtc,
            incident.CreatedAtUtc,
            incident.ResolvedAtUtc,
            incident.StatusUpdatedAtUtc,
            incident.ClosedBy,
            incident.ClosedAtUtc,
            incident.ClosureSummary,
            incident.FinalRcaInvestigationId,
            incident.ClosureValidationConfirmed);
    }
}

public sealed record RunbookSummaryResponse(
    int Id,
    string RunbookCode,
    string Title,
    string IntegrationName,
    RootCauseCategory Category,
    string EscalationTeam,
    DateTimeOffset UpdatedAtUtc)
{
    public static RunbookSummaryResponse FromEntity(Runbook runbook)
    {
        return new RunbookSummaryResponse(
            runbook.Id,
            runbook.RunbookCode,
            runbook.Title,
            runbook.IntegrationName,
            runbook.Category,
            runbook.EscalationTeam,
            runbook.UpdatedAtUtc);
    }
}
public sealed class CreateIncidentRequest
{
    public string IncidentNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IntegrationName { get; set; } = string.Empty;

    public string AffectedSystem { get; set; } = string.Empty;

    public string Environment { get; set; } =
        "Production Simulation";

    public string BusinessImpact { get; set; } = string.Empty;

    public string TechnicalEvidence { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? CorrelationId { get; set; }

    public IncidentSeverity Severity { get; set; }
        = IncidentSeverity.Medium;

    public IncidentStatus Status { get; set; }
        = IncidentStatus.Investigating;

    public RootCauseCategory RootCauseCategory { get; set; }
        = RootCauseCategory.Unknown;

    public string? ConfirmedResolution { get; set; }

    public DateTimeOffset? DetectedAtUtc { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }
}

public sealed class CloseIncidentRequest
{
    public string ClosedBy { get; set; } = string.Empty;

    public string ClosureSummary { get; set; } = string.Empty;

    public Guid FinalRcaInvestigationId { get; set; }

    public bool ValidationConfirmed { get; set; }
}

