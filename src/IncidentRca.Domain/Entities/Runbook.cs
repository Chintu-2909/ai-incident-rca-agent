using IncidentRca.Domain.Enums;

namespace IncidentRca.Domain.Entities;

public sealed class Runbook
{
    public int Id { get; set; }

    public string RunbookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string IntegrationName { get; set; } = string.Empty;

    public RootCauseCategory Category { get; set; }

    public string Symptoms { get; set; } = string.Empty;

    public string InvestigationSteps { get; set; } = string.Empty;

    public string CorrectiveActions { get; set; } = string.Empty;

    public string PreventiveActions { get; set; } = string.Empty;

    public string ValidationSteps { get; set; } = string.Empty;

    public string EscalationTeam { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;
}
