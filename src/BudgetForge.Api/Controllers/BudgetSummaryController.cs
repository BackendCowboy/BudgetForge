using BudgetForge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetForge.Api.Controllers;

[ApiController]
[Route("api/budget-summary")]
 
public class BudgetSummaryController : ControllerBase
{
    private readonly IBudgetSummaryService _svc;

    public BudgetSummaryController(IBudgetSummaryService svc) => _svc = svc;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int? accountId,
        CancellationToken ct)
    {
        if (from == default || to == default || from > to)
            return BadRequest(new { message = "Provide valid from/to query params" });

        var result = await _svc.GetSummaryAsync(from, to, accountId, ct);
        return Ok(result);
    }
}