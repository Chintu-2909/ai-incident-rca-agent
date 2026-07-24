using IncidentRca.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace IncidentRca.Api.Controllers;

[ApiController]
[Route("api/agent-tools")]
public sealed class AgentToolsController(
    IIncidentAgentTools agentTools,
    ILogger<AgentToolsController> logger)
    : ControllerBase
{
    [HttpGet("test/{incidentNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestToolsAsync(
        string incidentNumber,
        CancellationToken cancellationToken)
    {
        var normalizedIncidentNumber = incidentNumber
            .Trim()
            .ToUpperInvariant();

        logger.LogInformation(
            "Starting controlled agent-tool test for {IncidentNumber}",
            normalizedIncidentNumber);

        var incident =
            await agentTools.GetIncidentDetailsAsync(
                normalizedIncidentNumber,
                cancellationToken);

        if (incident is null)
        {
            return NotFound(new
            {
                message =
                    $"Incident {normalizedIncidentNumber} was not found."
            });
        }

        var similarIncidents =
            await agentTools.FindSimilarIncidentsAsync(
                normalizedIncidentNumber,
                cancellationToken);

        var runbook =
            await agentTools.GetIntegrationRunbookAsync(
                incident.IntegrationName,
                incident.KnownRootCauseCategory,
                cancellationToken);

        var evidenceValidation =
            await agentTools.ValidateIncidentEvidenceAsync(
                incident,
                cancellationToken);

        var executedTools = new[]
        {
            new
            {
                stepNumber = 1,
                toolName = "GetIncidentDetails",
                successful = true,
                summary =
                    $"Retrieved incident {incident.IncidentNumber}."
            },
            new
            {
                stepNumber = 2,
                toolName = "FindSimilarIncidents",
                successful = true,
                summary =
                    $"Found {similarIncidents.Count} related incidents."
            },
            new
            {
                stepNumber = 3,
                toolName = "GetIntegrationRunbook",
                successful = runbook is not null,
                summary = runbook is null
                    ? "No matching runbook was found."
                    : $"Retrieved runbook {runbook.RunbookCode}."
            },
            new
            {
                stepNumber = 4,
                toolName = "ValidateIncidentEvidence",
                successful = true,
                summary =
                    evidenceValidation.ValidationSummary
            }
        };

        logger.LogInformation(
            "Completed controlled agent-tool test for {IncidentNumber}",
            normalizedIncidentNumber);

        return Ok(new
        {
            incident,
            similarIncidents,
            runbook,
            evidenceValidation,
            executedTools
        });
    }
}
