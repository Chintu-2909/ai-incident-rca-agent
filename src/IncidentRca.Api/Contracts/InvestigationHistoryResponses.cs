using IncidentRca.Domain.Enums;

namespace IncidentRca.Api.Contracts;

public sealed record InvestigationHistoryResponse(
    Guid InvestigationId,
    string IncidentNumber,
    string IncidentTitle,
    string Objective,
    string AiProvider,
    string ModelName,
    InvestigationStatus Status,
    RootCauseCategory RootCauseCategory,
    ConfidenceLevel Confidence,
    int ToolExecutionCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? DurationMilliseconds,
    string? FailureReason);
