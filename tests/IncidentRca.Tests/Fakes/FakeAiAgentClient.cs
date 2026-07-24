using IncidentRca.Application.Abstractions;
using IncidentRca.Application.Models;
using IncidentRca.Domain.Enums;
using IncidentRca.Domain.Models;

namespace IncidentRca.Tests.Fakes;

internal sealed class FakeAiAgentClient : IAiAgentClient
{
    public string ProviderName => "FakeAI";

    public string ModelName => "fake-incident-model";

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public AiIncidentContext? LastContext { get; private set; }

    public Task<IncidentInvestigationResult> GenerateReportAsync(
        AiIncidentContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(context);

        CallCount++;
        LastContext = context;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        var result = new IncidentInvestigationResult
        {
            InvestigationId = context.InvestigationId,
            IncidentNumber =
                context.Incident.IncidentNumber,
            Classification = "Mapping Failure",
            RootCauseCategory =
                context.Incident.KnownRootCauseCategory,
            Confidence = ConfidenceLevel.High,
            ProbableRootCause =
                "The source department code did not have a valid target-system mapping.",
            ConfirmedFacts =
                context.EvidenceValidation.ConfirmedFacts.ToList(),
            Assumptions =
                context.EvidenceValidation.Assumptions.ToList(),
            MissingInformation =
                context.EvidenceValidation.MissingInformation.ToList(),
            Evidence =
                context.Incident.TechnicalEvidence
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .ToList(),
            TechnicalSummary =
                "The onboarding request failed because the department mapping was missing.",
            ServiceNowWorkNote =
                "Investigation confirmed a missing department mapping. The mapping was corrected and affected requests were reprocessed.",
            StakeholderUpdate =
                "The employee onboarding issue was identified and resolved. A missing system mapping prevented employee creation.",
            RootCauseAnalysis =
                "The target HCM system rejected the request because the FIN-OPS department mapping was unavailable.",
            CorrectiveActions =
            [
                "Add the approved department mapping.",
                "Reprocess the affected transactions."
            ],
            PreventiveActions =
            [
                "Validate mappings before processing.",
                "Alert on unknown department codes."
            ],
            ValidationChecklist =
            [
                "Confirm employee creation.",
                "Confirm the correct department identifier.",
                "Monitor subsequent transactions."
            ],
            ShiftHandoverNote =
                "The mapping issue was resolved. Continue monitoring according to the runbook.",
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult(result);
    }
}
