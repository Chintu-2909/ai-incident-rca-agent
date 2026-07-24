using IncidentRca.Api.Contracts;
using IncidentRca.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentRca.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public sealed class IncidentsController(
    IncidentRcaDbContext dbContext,
    ILogger<IncidentsController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<IncidentSummaryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncidentSummaryResponse>>>
        GetIncidentsAsync(CancellationToken cancellationToken)
    {
        var incidentEntities = await dbContext.Incidents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var incidents = incidentEntities
            .OrderByDescending(x => x.DetectedAtUtc)
            .Select(IncidentSummaryResponse.FromEntity)
            .ToList();

        logger.LogInformation(
            "Returned {IncidentCount} synthetic incidents",
            incidents.Count);

        return Ok(incidents);
    }

    [HttpPost]
    [ProducesResponseType<IncidentDetailsResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidentDetailsResponse>>
        CreateIncidentAsync(
            [FromBody] CreateIncidentRequest request,
            CancellationToken cancellationToken)
    {
        var validationErrors =
            ValidateCreateRequest(request);

        var createStatusError =
            ValidateCreateStatus(request.Status);

        if (createStatusError is not null)
        {
            validationErrors[nameof(request.Status)] =
                createStatusError;
        }

        var resolutionError =
            ValidateResolutionForStatus(
                request.Status,
                request.ConfirmedResolution);

        if (resolutionError is not null)
        {
            validationErrors[
                nameof(request.ConfirmedResolution)] =
                resolutionError;
        }

        if (validationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message =
                    "The incident request contains invalid data.",
                errors = validationErrors
            });
        }

        var incidentNumber = request.IncidentNumber
            .Trim()
            .ToUpperInvariant();

        var incidentExists = await dbContext.Incidents
            .AsNoTracking()
            .AnyAsync(
                incident =>
                    incident.IncidentNumber ==
                    incidentNumber,
                cancellationToken);

        if (incidentExists)
        {
            logger.LogWarning(
                "Incident {IncidentNumber} already exists",
                incidentNumber);

            return Conflict(new
            {
                message =
                    $"Incident {incidentNumber} already exists."
            });
        }

        var detectedAtUtc =
            request.DetectedAtUtc ??
            DateTimeOffset.UtcNow;

        var incident =
            new IncidentRca.Domain.Entities.Incident
            {
                IncidentNumber = incidentNumber,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                IntegrationName =
                    request.IntegrationName.Trim(),
                AffectedSystem =
                    request.AffectedSystem.Trim(),
                Environment =
                    request.Environment.Trim(),
                BusinessImpact =
                    request.BusinessImpact.Trim(),
                TechnicalEvidence =
                    request.TechnicalEvidence.Trim(),
                ErrorCode =
                    NormalizeOptional(request.ErrorCode),
                ErrorMessage =
                    NormalizeOptional(request.ErrorMessage),
                CorrelationId =
                    NormalizeOptional(request.CorrelationId),
                Severity = request.Severity,
                Status = request.Status,
                KnownRootCauseCategory =
                    request.RootCauseCategory,
                ConfirmedResolution =
                    NormalizeOptional(
                        request.ConfirmedResolution),
                DetectedAtUtc = detectedAtUtc,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ResolvedAtUtc =
                    request.Status is
                        IncidentRca.Domain.Enums
                            .IncidentStatus.Resolved
                        or
                        IncidentRca.Domain.Enums
                            .IncidentStatus.Closed
                            ? request.ResolvedAtUtc ??
                              DateTimeOffset.UtcNow
                            : null
            };

        dbContext.Incidents.Add(incident);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Created incident {IncidentNumber} with ID {IncidentId}",
            incident.IncidentNumber,
            incident.Id);

        var response =
            IncidentDetailsResponse.FromEntity(
                incident);

        var location =
            $"/api/incidents/{Uri.EscapeDataString(incident.IncidentNumber)}";

        return Created(
            location,
            response);
    }

    private static string? ValidateCreateStatus(
        IncidentRca.Domain.Enums.IncidentStatus status)
    {
        return status switch
        {
            IncidentRca.Domain.Enums.IncidentStatus.New =>
                null,

            IncidentRca.Domain.Enums.IncidentStatus.Investigating =>
                null,

            IncidentRca.Domain.Enums.IncidentStatus.Resolved =>
                "A new incident must first be investigated before it can be resolved.",

            IncidentRca.Domain.Enums.IncidentStatus.Closed =>
                "A new incident cannot be created as Closed. Use the controlled closure workflow.",

            _ =>
                "The requested incident status is not supported."
        };
    }

    private static string? ValidateStatusTransition(
        IncidentRca.Domain.Enums.IncidentStatus currentStatus,
        IncidentRca.Domain.Enums.IncidentStatus requestedStatus)
    {
        if (currentStatus ==
            IncidentRca.Domain.Enums.IncidentStatus.Closed)
        {
            return
                "Closed incidents are read-only and cannot be modified.";
        }

        if (requestedStatus ==
            IncidentRca.Domain.Enums.IncidentStatus.Closed)
        {
            return
                "Incidents can be closed only through the controlled closure workflow.";
        }

        if (currentStatus == requestedStatus)
        {
            return null;
        }

        var allowed =
            currentStatus switch
            {
                IncidentRca.Domain.Enums.IncidentStatus.New =>
                    requestedStatus ==
                    IncidentRca.Domain.Enums
                        .IncidentStatus.Investigating,

                IncidentRca.Domain.Enums
                    .IncidentStatus.Investigating =>
                    requestedStatus ==
                    IncidentRca.Domain.Enums
                        .IncidentStatus.Resolved,

                IncidentRca.Domain.Enums.IncidentStatus.Resolved =>
                    requestedStatus ==
                    IncidentRca.Domain.Enums
                        .IncidentStatus.Investigating,

                _ => false
            };

        return allowed
            ? null
            : $"Status transition from {currentStatus} to {requestedStatus} is not allowed.";
    }

    private static string?
        ValidateResolutionForStatus(
            IncidentRca.Domain.Enums.IncidentStatus status,
            string? confirmedResolution)
    {
        if (status is not
                IncidentRca.Domain.Enums.IncidentStatus.Resolved
            and not
                IncidentRca.Domain.Enums.IncidentStatus.Closed)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(
            confirmedResolution)
                ? "A confirmed resolution is required for a resolved or closed incident."
                : null;
    }

    private static Dictionary<string, string>
        ValidateCreateRequest(
            CreateIncidentRequest request)
    {
        var errors =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        ValidateRequired(
            errors,
            nameof(request.IncidentNumber),
            request.IncidentNumber,
            30);

        ValidateRequired(
            errors,
            nameof(request.Title),
            request.Title,
            200);

        ValidateRequired(
            errors,
            nameof(request.Description),
            request.Description,
            4000);

        ValidateRequired(
            errors,
            nameof(request.IntegrationName),
            request.IntegrationName,
            150);

        ValidateRequired(
            errors,
            nameof(request.AffectedSystem),
            request.AffectedSystem,
            100);

        ValidateRequired(
            errors,
            nameof(request.Environment),
            request.Environment,
            50);

        ValidateRequired(
            errors,
            nameof(request.BusinessImpact),
            request.BusinessImpact,
            2000);

        ValidateRequired(
            errors,
            nameof(request.TechnicalEvidence),
            request.TechnicalEvidence,
            8000);

        ValidateOptional(
            errors,
            nameof(request.ErrorCode),
            request.ErrorCode,
            100);

        ValidateOptional(
            errors,
            nameof(request.ErrorMessage),
            request.ErrorMessage,
            2000);

        ValidateOptional(
            errors,
            nameof(request.CorrelationId),
            request.CorrelationId,
            100);

        ValidateOptional(
            errors,
            nameof(request.ConfirmedResolution),
            request.ConfirmedResolution,
            4000);

        if (!string.IsNullOrWhiteSpace(
                request.IncidentNumber) &&
            !request.IncidentNumber
                .Trim()
                .StartsWith(
                    "INC-",
                    StringComparison.OrdinalIgnoreCase))
        {
            errors[nameof(request.IncidentNumber)] =
                "Incident number must begin with INC-.";
        }

        if (request.ResolvedAtUtc.HasValue &&
            request.DetectedAtUtc.HasValue &&
            request.ResolvedAtUtc.Value <
            request.DetectedAtUtc.Value)
        {
            errors[nameof(request.ResolvedAtUtc)] =
                "Resolved time cannot be earlier than detected time.";
        }

        return errors;
    }

    private static void ValidateRequired(
        IDictionary<string, string> errors,
        string field,
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] =
                $"{field} is required.";

            return;
        }

        if (value.Trim().Length > maximumLength)
        {
            errors[field] =
                $"{field} cannot exceed {maximumLength} characters.";
        }
    }

    private static void ValidateOptional(
        IDictionary<string, string> errors,
        string field,
        string? value,
        int maximumLength)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            value.Trim().Length > maximumLength)
        {
            errors[field] =
                $"{field} cannot exceed {maximumLength} characters.";
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }


    [HttpPost("{incidentNumber}/close")]
    [ProducesResponseType<IncidentDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidentDetailsResponse>>
        CloseIncidentAsync(
            string incidentNumber,
            [FromBody] CloseIncidentRequest request,
            CancellationToken cancellationToken)
    {
        var normalizedIncidentNumber =
            incidentNumber
                .Trim()
                .ToUpperInvariant();

        var errors =
            new List<string>();

        if (request is null)
        {
            return BadRequest(new
            {
                message =
                    "The closure request is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ClosedBy))
        {
            errors.Add(
                "The closure owner is required.");
        }
        else if (request.ClosedBy.Trim().Length > 200)
        {
            errors.Add(
                "The closure owner cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(
                request.ClosureSummary))
        {
            errors.Add(
                "The closure summary is required.");
        }
        else if (request.ClosureSummary.Trim().Length > 4000)
        {
            errors.Add(
                "The closure summary cannot exceed 4000 characters.");
        }

        if (request.FinalRcaInvestigationId ==
            Guid.Empty)
        {
            errors.Add(
                "A final RCA investigation is required.");
        }

        if (!request.ValidationConfirmed)
        {
            errors.Add(
                "Closure validation must be confirmed.");
        }

        if (errors.Count > 0)
        {
            return BadRequest(new
            {
                message =
                    "The incident closure request contains invalid data.",
                errors
            });
        }

        var incident = await dbContext.Incidents
            .SingleOrDefaultAsync(
                item =>
                    item.IncidentNumber ==
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

        if (incident.Status ==
            IncidentRca.Domain.Enums.IncidentStatus.Closed)
        {
            return Conflict(new
            {
                message =
                    $"Incident {normalizedIncidentNumber} is already closed and read-only."
            });
        }

        if (incident.Status !=
            IncidentRca.Domain.Enums.IncidentStatus.Resolved)
        {
            return Conflict(new
            {
                message =
                    $"Incident {normalizedIncidentNumber} cannot be closed.",
                errors = new[]
                {
                    "Only a Resolved incident can enter the controlled closure workflow."
                }
            });
        }

        if (string.IsNullOrWhiteSpace(
                incident.ConfirmedResolution))
        {
            errors.Add(
                "A confirmed resolution is required before closure.");
        }

        if (incident.KnownRootCauseCategory ==
            IncidentRca.Domain.Enums.RootCauseCategory.Unknown)
        {
            errors.Add(
                "A confirmed root-cause category is required before closure.");
        }

        var finalRca = await dbContext.Investigations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                investigation =>
                    investigation.Id ==
                    request.FinalRcaInvestigationId,
                cancellationToken);

        if (finalRca is null)
        {
            errors.Add(
                "The selected final RCA investigation was not found.");
        }
        else
        {
            if (finalRca.IncidentId != incident.Id)
            {
                errors.Add(
                    "The selected final RCA does not belong to this incident.");
            }

            if (finalRca.Status !=
                IncidentRca.Domain.Enums
                    .InvestigationStatus.Completed)
            {
                errors.Add(
                    "The selected final RCA investigation is not completed.");
            }

            if (finalRca.Confidence ==
                IncidentRca.Domain.Enums
                    .ConfidenceLevel.InsufficientEvidence)
            {
                errors.Add(
                    "An investigation with Insufficient Evidence cannot be approved as the final RCA.");
            }

            if (string.IsNullOrWhiteSpace(
                    finalRca.ResultJson) ||
                finalRca.ResultJson.Trim() == "{}")
            {
                errors.Add(
                    "The selected final RCA does not contain a saved report.");
            }
        }

        if (errors.Count > 0)
        {
            return Conflict(new
            {
                message =
                    $"Incident {normalizedIncidentNumber} cannot be closed.",
                errors
            });
        }

        var closedAtUtc =
            DateTimeOffset.UtcNow;

        incident.Status =
            IncidentRca.Domain.Enums.IncidentStatus.Closed;

        incident.ClosedBy =
            request.ClosedBy.Trim();

        incident.ClosedAtUtc =
            closedAtUtc;

        incident.ClosureSummary =
            request.ClosureSummary.Trim();

        incident.FinalRcaInvestigationId =
            request.FinalRcaInvestigationId;

        incident.ClosureValidationConfirmed =
            true;

        incident.StatusUpdatedAtUtc =
            closedAtUtc;

        incident.ResolvedAtUtc ??=
            closedAtUtc;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Closed incident {IncidentNumber} using final RCA {InvestigationId} by {ClosedBy}",
            incident.IncidentNumber,
            incident.FinalRcaInvestigationId,
            incident.ClosedBy);

        return Ok(
            IncidentDetailsResponse.FromEntity(
                incident));
    }


    [HttpPut("{incidentNumber}")]
    [ProducesResponseType<IncidentDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidentDetailsResponse>>
        UpdateIncidentAsync(
            string incidentNumber,
            [FromBody] CreateIncidentRequest request,
            CancellationToken cancellationToken)
    {
        var routeIncidentNumber = incidentNumber
            .Trim()
            .ToUpperInvariant();

        var requestIncidentNumber = request.IncidentNumber
            .Trim()
            .ToUpperInvariant();

        if (!string.Equals(
                routeIncidentNumber,
                requestIncidentNumber,
                StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new
            {
                message =
                    "The route incident number must match the request incident number."
            });
        }

        var validationErrors =
            ValidateCreateRequest(request);

        if (validationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message =
                    "The incident request contains invalid data.",
                errors = validationErrors
            });
        }

        var incident = await dbContext.Incidents
            .SingleOrDefaultAsync(
                item =>
                    item.IncidentNumber ==
                    routeIncidentNumber,
                cancellationToken);

        if (incident is null)
        {
            return NotFound(new
            {
                message =
                    $"Incident {routeIncidentNumber} was not found."
            });
        }

        var transitionError =
            ValidateStatusTransition(
                incident.Status,
                request.Status);

        if (transitionError is not null)
        {
            return Conflict(new
            {
                message =
                    "The requested incident status change is not allowed.",
                detail = transitionError
            });
        }

        var resolutionError =
            ValidateResolutionForStatus(
                request.Status,
                request.ConfirmedResolution);

        if (resolutionError is not null)
        {
            return BadRequest(new
            {
                message =
                    "The incident request contains invalid data.",
                errors = new Dictionary<string, string>
                {
                    [nameof(request.ConfirmedResolution)] =
                        resolutionError
                }
            });
        }

        var statusChanged =
            incident.Status != request.Status;

        incident.Title = request.Title.Trim();
        incident.Description = request.Description.Trim();
        incident.IntegrationName =
            request.IntegrationName.Trim();
        incident.AffectedSystem =
            request.AffectedSystem.Trim();
        incident.Environment =
            request.Environment.Trim();
        incident.BusinessImpact =
            request.BusinessImpact.Trim();
        incident.TechnicalEvidence =
            request.TechnicalEvidence.Trim();
        incident.ErrorCode =
            NormalizeOptional(request.ErrorCode);
        incident.ErrorMessage =
            NormalizeOptional(request.ErrorMessage);
        incident.CorrelationId =
            NormalizeOptional(request.CorrelationId);
        incident.Severity = request.Severity;
        incident.Status = request.Status;
        incident.KnownRootCauseCategory =
            request.RootCauseCategory;
        incident.ConfirmedResolution =
            NormalizeOptional(
                request.ConfirmedResolution);

        if (request.DetectedAtUtc.HasValue)
        {
            incident.DetectedAtUtc =
                request.DetectedAtUtc.Value;
        }

        if (request.Status ==
            IncidentRca.Domain.Enums
                .IncidentStatus.Resolved)
        {
            incident.ResolvedAtUtc =
                request.ResolvedAtUtc ??
                incident.ResolvedAtUtc ??
                DateTimeOffset.UtcNow;
        }
        else
        {
            incident.ResolvedAtUtc = null;
        }

        if (statusChanged)
        {
            incident.StatusUpdatedAtUtc =
                DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Updated incident {IncidentNumber}",
            incident.IncidentNumber);

        return Ok(
            IncidentDetailsResponse.FromEntity(
                incident));
    }


    [HttpGet("{incidentNumber}")]
    [ProducesResponseType<IncidentDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDetailsResponse>>
        GetIncidentAsync(
            string incidentNumber,
            CancellationToken cancellationToken)
    {
        var normalizedIncidentNumber = incidentNumber
            .Trim()
            .ToUpperInvariant();

        var incident = await dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IncidentNumber == normalizedIncidentNumber,
                cancellationToken);

        if (incident is null)
        {
            logger.LogWarning(
                "Incident {IncidentNumber} was not found",
                normalizedIncidentNumber);

            return NotFound(new
            {
                message =
                    $"Incident {normalizedIncidentNumber} was not found."
            });
        }

        return Ok(IncidentDetailsResponse.FromEntity(incident));
    }
}
