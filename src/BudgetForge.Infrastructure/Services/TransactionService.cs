using BudgetForge.Application.DTOs;
using BudgetForge.Application.Interfaces;
using BudgetForge.Domain.Entities;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetForge.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;

        public TransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionResponse> CreateTransactionAsync(int userId, CreateTransactionRequest request)
        {
            // First verify the account belongs to the user
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId && !a.IsDeleted);
            
            if (account == null)
            {
                throw new UnauthorizedAccessException("Account not found or doesn't belong to user");
            }

            var transaction = new Transaction
            {
                AccountId = request.AccountId,
                Type = request.Type,
                Description = request.Description,
                Amount = request.Amount,
                Date = request.Date ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Transactions.Add(transaction);
            
            // Update account balance based on transaction type
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance += transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance -= transaction.Amount;
            }
            
            account.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return MapToResponse(transaction);
        }

        public async Task<IEnumerable<TransactionResponse>> GetTransactionsAsync(int userId)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account!.UserId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return transactions.Select(MapToResponse);
        }

        public async Task<TransactionResponse?> GetTransactionByIdAsync(int userId, int transactionId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId && 
                                          t.Account!.UserId == userId && 
                                          !t.IsDeleted);

            return transaction != null ? MapToResponse(transaction) : null;
        }

        public async Task<IEnumerable<TransactionResponse>> GetTransactionsByAccountAsync(int userId, int accountId)
        {
            // Verify account belongs to user first
            var accountExists = await _context.Accounts
                .AnyAsync(a => a.Id == accountId && a.UserId == userId && !a.IsDeleted);
            
            if (!accountExists)
            {
                return Enumerable.Empty<TransactionResponse>();
            }

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return transactions.Select(MapToResponse);
        }

        public async Task<bool> UpdateTransactionAsync(int userId, int transactionId, UpdateTransactionRequest request)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId && 
                                          t.Account!.UserId == userId && 
                                          !t.IsDeleted);

            if (transaction == null)
                return false;

            var account = transaction.Account!;
            
            // Reverse the old transaction's effect on balance
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance += transaction.Amount;
            }

            // Update transaction
            transaction.Type = request.Type;
            transaction.Description = request.Description;
            transaction.Amount = request.Amount;
            transaction.Date = request.Date;
            transaction.UpdatedAt = DateTime.UtcNow;

            // Apply the new transaction's effect on balance
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance += transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance -= transaction.Amount;
            }
            
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTransactionAsync(int userId, int transactionId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId && 
                                          t.Account!.UserId == userId && 
                                          !t.IsDeleted);

            if (transaction == null)
                return false;

            var account = transaction.Account!;
            
            // Reverse the transaction's effect on balance
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance += transaction.Amount;
            }

            transaction.IsDeleted = true;
            transaction.UpdatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        private TransactionResponse MapToResponse(Transaction transaction)
        {
            return new TransactionResponse
            {
                Id = transaction.Id,
                AccountId = transaction.AccountId,
                Type = transaction.Type,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }
    }
}