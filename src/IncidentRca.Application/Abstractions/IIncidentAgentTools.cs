using IncidentRca.Application.Models;
using IncidentRca.Domain.Enums;

namespace IncidentRca.Application.Abstractions;

public interface IIncidentAgentTools
{
    Task<IncidentToolResult?> GetIncidentDetailsAsync(
        string incidentNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SimilarIncidentResult>> FindSimilarIncidentsAsync(
        string incidentNumber,
        CancellationToken cancellationToken = default);

    Task<RunbookToolResult?> GetIntegrationRunbookAsync(
        string integrationName,
        RootCauseCategory category,
        CancellationToken cancellationToken = default);

    Task<EvidenceValidationToolResult> ValidateIncidentEvidenceAsync(
        IncidentToolResult incident,
        CancellationToken cancellationToken = default);
}
