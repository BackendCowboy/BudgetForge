using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BudgetForge.Application.Interfaces;
using BudgetForge.Application.DTOs;
using System.Security.Claims;

namespace BudgetForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // Helper method to get the current user's ID from JWT claims
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return int.Parse(userIdClaim);
        }

        /// <summary>
        /// Get all accounts for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAccounts()
        {
            var userId = GetUserId();
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            return Ok(accounts);
        }

        /// <summary>
        /// Get a specific account by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponse>> GetAccount(int id)
        {
            var userId = GetUserId();
            var account = await _accountService.GetAccountByIdAsync(userId, id);
            
            if (account == null)
            {
                return NotFound(new { message = "Account not found" });
            }
            
            return Ok(account);
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var account = await _accountService.CreateAccountAsync(userId, request);
            
            return CreatedAtAction(
                nameof(GetAccount), 
                new { id = account.Id }, 
                account
            );
        }

        /// <summary>
        /// Update an account (name and type only)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var result = await _accountService.UpdateAccountAsync(userId, id, request);
            
            if (!result)
            {
                return NotFound(new { message = "Account not found" });
            }
            
            return NoContent();
        }

        /// <summary>
        /// Soft delete an account
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userId = GetUserId();
            var result = await _accountService.SoftDeleteAccountAsync(userId, id);
            
            if (!result)
            {
                return NotFound(new { message = "Account not found or already deleted" });
            }
            
            return NoContent();
        }

        /// <summary>
        /// Get account balance summary for the current user
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult> GetAccountsSummary()
        {
            var userId = GetUserId();
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            
            var summary = new
            {
                TotalAccounts = accounts.Count(),
                TotalBalance = accounts.Sum(a => a.Balance),
                AccountsByType = accounts.GroupBy(a => a.Type)
                    .Select(g => new 
                    { 
                        Type = g.Key.ToString(), 
                        Count = g.Count(), 
                        TotalBalance = g.Sum(a => a.Balance) 
                    })
            };
            
            return Ok(summary);
        }
    }
}