namespace IncidentRca.Application.Models;

public sealed class AiIncidentContext
{
    public Guid InvestigationId { get; set; }

    public string Objective { get; set; } = string.Empty;

    public IncidentToolResult Incident { get; set; } = new();

    public IReadOnlyList<SimilarIncidentResult> SimilarIncidents
    {
        get;
        set;
    } = [];

    public RunbookToolResult? Runbook { get; set; }

    public EvidenceValidationToolResult EvidenceValidation
    {
        get;
        set;
    } = new();
}
