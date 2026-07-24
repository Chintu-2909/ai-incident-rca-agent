namespace IncidentRca.Domain.Enums;

public enum IncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum IncidentStatus
{
    New = 1,
    Investigating = 2,
    Resolved = 3,
    Closed = 4
}

public enum RootCauseCategory
{
    Unknown = 0,
    MappingFailure = 1,
    AuthenticationFailure = 2,
    AuthorizationFailure = 3,
    ValidationFailure = 4,
    DownstreamTimeout = 5,
    DuplicateProcessing = 6,
    DatabaseConnectivity = 7,
    ConfigurationFailure = 8,
    DownstreamOutage = 9
}

public enum ConfidenceLevel
{
    InsufficientEvidence = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public enum InvestigationStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

public enum ToolExecutionStatus
{
    Started = 1,
    Completed = 2,
    Failed = 3,
    Rejected = 4
}
