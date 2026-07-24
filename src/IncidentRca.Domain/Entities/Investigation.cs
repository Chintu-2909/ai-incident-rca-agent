using IncidentRca.Domain.Enums;

namespace IncidentRca.Domain.Entities;

public sealed class Investigation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int IncidentId { get; set; }

    public Incident? Incident { get; set; }

    public string Objective { get; set; } = string.Empty;

    public string AiProvider { get; set; } = "Ollama";

    public string ModelName { get; set; } = string.Empty;

    public InvestigationStatus Status { get; set; }
        = InvestigationStatus.Pending;

    public RootCauseCategory RootCauseCategory { get; set; }
        = RootCauseCategory.Unknown;

    public ConfidenceLevel Confidence { get; set; }
        = ConfidenceLevel.InsufficientEvidence;

    public string ResultJson { get; set; } = "{}";

    public string? FailureReason { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public ICollection<ToolExecution> ToolExecutions { get; set; }
        = new List<ToolExecution>();
}
