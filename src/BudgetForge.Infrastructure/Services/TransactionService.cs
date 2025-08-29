using BudgetForge.Application.DTOs;
using BudgetForge.Application.Interfaces;
using BudgetForge.Domain.Entities;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace BudgetForge.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TransactionService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private static decimal SignedAmount(TransactionType type, decimal amount)
        {
            var abs = Math.Abs(amount);
            return type == TransactionType.Expense ? -abs : abs;
        }

        public async Task<TransactionResponse> CreateTransactionAsync(int userId, CreateTransactionRequest request)
        {
            // Verify account ownership
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId && !a.IsDeleted);

            if (account == null)
                throw new UnauthorizedAccessException("Account not found or not owned by user.");

            var signed = SignedAmount(request.Type, request.Amount);


            var tx = new Transaction
            {
                AccountId = request.AccountId,
                Type = request.Type,
                Description = request.Description ?? string.Empty,
                Amount = signed,
                Date = request.Timestamp ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            account.Balance += signed;
            account.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(tx);
            await _context.SaveChangesAsync();

            return _mapper.Map<TransactionResponse>(tx);
        }

        public async Task<IEnumerable<TransactionResponse>> GetTransactionsAsync(int userId)
        {
            var list = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account!.UserId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return _mapper.Map<List<TransactionResponse>>(list);
        }

        public async Task<TransactionResponse?> GetTransactionByIdAsync(int userId, int transactionId)
        {
            var tx = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId &&
                                          t.Account!.UserId == userId &&
                                          !t.IsDeleted);

            return tx != null ? _mapper.Map<TransactionResponse>(tx) : null;
        }

        public async Task<IEnumerable<TransactionResponse>> GetTransactionsByAccountAsync(int userId, int accountId)
        {
            var exists = await _context.Accounts
                .AnyAsync(a => a.Id == accountId && a.UserId == userId && !a.IsDeleted);

            if (!exists) return Enumerable.Empty<TransactionResponse>();

            var list = await _context.Transactions
                .Where(t => t.AccountId == accountId && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return _mapper.Map<List<TransactionResponse>>(list);
        }

        public async Task<bool> UpdateTransactionAsync(int userId, int transactionId, UpdateTransactionRequest request)
        {
            var tx = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId &&
                                          t.Account!.UserId == userId &&
                                          !t.IsDeleted);

            if (tx == null) return false;

            var account = tx.Account!;

            // Reverse old effect
account.Balance -= SignedAmount(tx.Type, tx.Amount);

// Apply partial updates
if (request.Type.HasValue)      tx.Type = request.Type.Value;
if (request.Amount.HasValue)    tx.Amount = request.Amount.Value;
if (request.Description != null) tx.Description = request.Description;
if (request.Timestamp.HasValue) tx.Date = request.Timestamp.Value;

// Re-normalize stored amount based on (possibly) new Type/Amount
tx.Amount = SignedAmount(tx.Type, tx.Amount);

tx.UpdatedAt = DateTime.UtcNow;

// Apply new effect
account.Balance += SignedAmount(tx.Type, tx.Amount);
account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTransactionAsync(int userId, int transactionId)
        {
            // Soft delete + reverse effect
            var tx = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId &&
                                          t.Account!.UserId == userId &&
                                          !t.IsDeleted);

            if (tx == null) return false;

            var account = tx.Account!;

            account.Balance -= SignedAmount(tx.Type, tx.Amount);

            tx.IsDeleted = true;
            tx.UpdatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

    
    }
}