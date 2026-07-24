using IncidentRca.Api.Contracts;
using IncidentRca.Domain.Enums;
using IncidentRca.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentRca.Api.Controllers;

[ApiController]
[Route("api/runbooks")]
public sealed class RunbooksController(
    IncidentRcaDbContext dbContext)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RunbookSummaryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RunbookSummaryResponse>>>
        GetRunbooksAsync(
            [FromQuery] RootCauseCategory? category,
            CancellationToken cancellationToken)
    {
        var query = dbContext.Runbooks
            .AsNoTracking()
            .AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value);
        }

        var runbooks = await query
            .OrderBy(x => x.RunbookCode)
            .Select(x => RunbookSummaryResponse.FromEntity(x))
            .ToListAsync(cancellationToken);

        return Ok(runbooks);
    }

    [HttpGet("{runbookCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunbookAsync(
        string runbookCode,
        CancellationToken cancellationToken)
    {
        var normalizedRunbookCode = runbookCode
            .Trim()
            .ToUpperInvariant();

        var runbook = await dbContext.Runbooks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.RunbookCode == normalizedRunbookCode,
                cancellationToken);

        if (runbook is null)
        {
            return NotFound(new
            {
                message =
                    $"Runbook {normalizedRunbookCode} was not found."
            });
        }

        return Ok(runbook);
    }
}
