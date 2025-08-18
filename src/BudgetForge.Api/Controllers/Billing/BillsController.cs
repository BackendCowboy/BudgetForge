using System.Security.Claims;
using BudgetForge.Application.DTOs.Billing;
using BudgetForge.Application.Interfaces.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetForge.Api.Controllers.Billing;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly IBillQueries _queries;
    private readonly IBillCommands _commands;

    public BillsController(IBillQueries queries, IBillCommands commands)
    {
        _queries = queries;
        _commands = commands;
    }

    // GET /api/billing/upcoming?days=30
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

        days = days <= 0 ? 30 : days > 90 ? 90 : days;

        var items = await _queries.GetUpcomingAsync(userId, days, ct);
        return Ok(items);
    }

    // POST /api/billing/bills
    [HttpPost("bills")]
    public async Task<IActionResult> CreateBill([FromBody] BillCreateDto dto, CancellationToken ct)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

        var id = await _commands.CreateAsync(userId, dto, ct);
        return CreatedAtAction(nameof(GetUpcoming), new { days = 30 }, new { id });
    }

    // POST /api/billing/bills/{id}/pay
[HttpPost("bills/{id:guid}/pay")]
public async Task<IActionResult> Pay(Guid id, [FromBody] BillPaymentCreateDto dto, CancellationToken ct)
{
    var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirst("nameid")?.Value
               ?? User.FindFirst("sub")?.Value;

    if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

    var paymentId = await _commands.PayAsync(userId, id, dto, ct);
    return Ok(new { paymentId });
}
}