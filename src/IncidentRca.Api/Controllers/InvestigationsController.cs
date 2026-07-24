using IncidentRca.Api.Contracts;
using IncidentRca.Application.Abstractions;
using IncidentRca.Domain.Models;
using IncidentRca.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentRca.Api.Controllers;

[ApiController]
[Route("api/investigations")]
public sealed class InvestigationsController(
    IIncidentInvestigationService investigationService,
    IncidentRcaDbContext dbContext,
    ILogger<InvestigationsController> logger)
    : ControllerBase
{
    [HttpGet("history")]
    [ProducesResponseType<
        IReadOnlyList<InvestigationHistoryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        IReadOnlyList<InvestigationHistoryResponse>>>
        GetHistoryAsync(
            [FromQuery] string? incidentNumber,
            CancellationToken cancellationToken)
    {
        var query = dbContext.Investigations
            .AsNoTracking()
            .Include(investigation =>
                investigation.Incident)
            .Include(investigation =>
                investigation.ToolExecutions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(incidentNumber))
        {
            var normalizedIncidentNumber =
                incidentNumber
                    .Trim()
                    .ToUpperInvariant();

            query = query.Where(investigation =>
                investigation.Incident != null &&
                investigation.Incident.IncidentNumber ==
                normalizedIncidentNumber);
        }

        var investigationEntities =
            await query.ToListAsync(cancellationToken);

        var investigations = investigationEntities
            .OrderByDescending(investigation =>
                investigation.StartedAtUtc)
            .ToList();

        var response = investigations
            .Select(investigation =>
            {
                long? durationMilliseconds = null;

                if (investigation.CompletedAtUtc.HasValue)
                {
                    durationMilliseconds =
                        (long)(
                            investigation.CompletedAtUtc.Value -
                            investigation.StartedAtUtc)
                        .TotalMilliseconds;
                }

                return new InvestigationHistoryResponse(
                    investigation.Id,
                    investigation.Incident?
                        .IncidentNumber ?? "Unknown",
                    investigation.Incident?
                        .Title ?? "Unknown incident",
                    investigation.Objective,
                    investigation.AiProvider,
                    investigation.ModelName,
                    investigation.Status,
                    investigation.RootCauseCategory,
                    investigation.Confidence,
                    investigation.ToolExecutions.Count,
                    investigation.StartedAtUtc,
                    investigation.CompletedAtUtc,
                    durationMilliseconds,
                    investigation.FailureReason);
            })
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<IncidentInvestigationResult>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IncidentInvestigationResult>>
        CreateInvestigationAsync(
            [FromBody] StartInvestigationRequest request,
            CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message =
                    "The investigation request is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.IncidentNumber))
        {
            return BadRequest(new
            {
                message =
                    "Incident number is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.Objective))
        {
            return BadRequest(new
            {
                message =
                    "Investigation objective is required."
            });
        }

        try
        {
            var result =
                await investigationService.InvestigateAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
            when (exception.Message.Contains(
                "closed and read-only",
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Rejected investigation for closed incident: {Message}",
                exception.Message);

            return Conflict(new
            {
                message = exception.Message
            });
        }

        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Investigation request was cancelled");

            return StatusCode(
                StatusCodes.Status499ClientClosedRequest);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(
                exception,
                "AI provider failed while generating the investigation report");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The AI provider could not generate a valid investigation report.",
                    detail = exception.Message
                });
        }
    }

    [HttpGet("{investigationId:guid}")]
    [ProducesResponseType<IncidentInvestigationResult>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentInvestigationResult>>
        GetInvestigationAsync(
            Guid investigationId,
            CancellationToken cancellationToken)
    {
        var result =
            await investigationService.GetInvestigationAsync(
                investigationId,
                cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message =
                    $"Completed investigation {investigationId} was not found."
            });
        }

        return Ok(result);
    }
}
