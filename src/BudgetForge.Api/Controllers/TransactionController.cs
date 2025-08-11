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
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
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
        /// Get all transactions for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactions()
        {
            var userId = GetUserId();
            var transactions = await _transactionService.GetTransactionsAsync(userId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get transactions for a specific account
        /// </summary>
        [HttpGet("account/{accountId}")]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactionsByAccount(int accountId)
        {
            var userId = GetUserId();
            var transactions = await _transactionService.GetTransactionsByAccountAsync(userId, accountId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get a specific transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponse>> GetTransaction(int id)
        {
            var userId = GetUserId();
            var transaction = await _transactionService.GetTransactionByIdAsync(userId, id);
            
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }
            
            return Ok(transaction);
        }

        /// <summary>
        /// Create a new transaction
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetUserId();
                var transaction = await _transactionService.CreateTransactionAsync(userId, request);
                
                return CreatedAtAction(
                    nameof(GetTransaction), 
                    new { id = transaction.Id }, 
                    transaction
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing transaction
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var result = await _transactionService.UpdateTransactionAsync(userId, id, request);
            
            if (!result)
            {
                return NotFound(new { message = "Transaction not found" });
            }
            
            return NoContent();
        }

        /// <summary>
        /// Delete a transaction (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = GetUserId();
            var result = await _transactionService.DeleteTransactionAsync(userId, id);
            
            if (!result)
            {
                return NotFound(new { message = "Transaction not found" });
            }
            
            return NoContent();
        }

        /// <summary>
        /// Get transaction summary for the current user
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult> GetTransactionsSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var userId = GetUserId();
            var transactions = await _transactionService.GetTransactionsAsync(userId);
            
            // Filter by date range if provided
            if (startDate.HasValue)
            {
                transactions = transactions.Where(t => t.Timestamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                transactions = transactions.Where(t => t.Timestamp <= endDate.Value);
            }
            
            var transactionsList = transactions.ToList();
            
            var summary = new
            {
                TotalTransactions = transactionsList.Count,
                TotalIncome = transactionsList
                    .Where(t => t.Type == Domain.Entities.TransactionType.Income)
                    .Sum(t => t.Amount),
                TotalExpenses = transactionsList
                    .Where(t => t.Type == Domain.Entities.TransactionType.Expense)
                    .Sum(t => t.Amount),
                NetAmount = transactionsList
                    .Where(t => t.Type == Domain.Entities.TransactionType.Income)
                    .Sum(t => t.Amount) - 
                    transactionsList
                    .Where(t => t.Type == Domain.Entities.TransactionType.Expense)
                    .Sum(t => t.Amount),
                TransactionsByType = transactionsList
                    .GroupBy(t => t.Type)
                    .Select(g => new 
                    { 
                        Type = g.Key.ToString(), 
                        Count = g.Count(), 
                        TotalAmount = g.Sum(t => t.Amount) 
                    }),
                StartDate = startDate,
                EndDate = endDate
            };
            
            return Ok(summary);
        }

        /// <summary>
        /// Get transactions for a specific date range
        /// </summary>
        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactionsByDateRange(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            var userId = GetUserId();
            var transactions = await _transactionService.GetTransactionsAsync(userId);
            
            var filteredTransactions = transactions
                .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate)
                .OrderByDescending(t => t.Timestamp);
            
            return Ok(filteredTransactions);
        }
    }
}