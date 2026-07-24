using IncidentRca.Domain.Models;

namespace IncidentRca.Application.Abstractions;

public interface IIncidentInvestigationService
{
    Task<IncidentInvestigationResult> InvestigateAsync(
        StartInvestigationRequest request,
        CancellationToken cancellationToken = default);

    Task<IncidentInvestigationResult?> GetInvestigationAsync(
        Guid investigationId,
        CancellationToken cancellationToken = default);
}
