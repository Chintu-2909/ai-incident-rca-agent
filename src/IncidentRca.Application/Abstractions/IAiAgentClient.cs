using IncidentRca.Application.Models;
using IncidentRca.Domain.Models;

namespace IncidentRca.Application.Abstractions;

public interface IAiAgentClient
{
    string ProviderName { get; }

    string ModelName { get; }

    Task<IncidentInvestigationResult> GenerateReportAsync(
        AiIncidentContext context,
        CancellationToken cancellationToken = default);
}
