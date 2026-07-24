using IncidentRca.Domain.Enums;

namespace IncidentRca.Domain.Entities;

public sealed class Incident
{
    public int Id { get; set; }

    public string IncidentNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IntegrationName { get; set; } = string.Empty;

    public string AffectedSystem { get; set; } = string.Empty;

    public string Environment { get; set; } = "Production Simulation";

    public string BusinessImpact { get; set; } = string.Empty;

    public string TechnicalEvidence { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? CorrelationId { get; set; }

    public IncidentSeverity Severity { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.New;

    public RootCauseCategory KnownRootCauseCategory { get; set; }
        = RootCauseCategory.Unknown;

    public string? ConfirmedResolution { get; set; }

    public DateTimeOffset DetectedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public DateTimeOffset? StatusUpdatedAtUtc { get; set; }

    public string? ClosedBy { get; set; }

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public string? ClosureSummary { get; set; }

    public Guid? FinalRcaInvestigationId { get; set; }

    public bool ClosureValidationConfirmed { get; set; }
}
