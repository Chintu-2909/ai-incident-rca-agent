using IncidentRca.Domain.Enums;

namespace IncidentRca.Domain.Entities;

public sealed class ToolExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvestigationId { get; set; }

    public Investigation? Investigation { get; set; }

    public int StepNumber { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = "{}";

    public string? ResultJson { get; set; }

    public ToolExecutionStatus Status { get; set; }
        = ToolExecutionStatus.Started;

    public string? ErrorMessage { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public long DurationMilliseconds { get; set; }
}
