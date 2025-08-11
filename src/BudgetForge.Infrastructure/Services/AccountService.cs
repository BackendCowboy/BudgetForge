using BudgetForge.Application.DTOs;
using BudgetForge.Application.Interfaces;
using BudgetForge.Domain.Entities;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetForge.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;

        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccountResponse> CreateAccountAsync(int userId, CreateAccountRequest request)
        {
            var account = new Account
            {
                UserId = userId,
                Name = request.Name,
                Type = request.Type,
                Balance = request.InitialBalance,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return new AccountResponse
            {
                Id = account.Id,
                Name = account.Name,
                Type = account.Type,
                Balance = account.Balance,
                Currency = account.Currency,
                IsDeleted = account.IsDeleted,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            };
        }

        public async Task<IEnumerable<AccountResponse>> GetUserAccountsAsync(int userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .Select(a => new AccountResponse
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Balance = a.Balance,
                    Currency = a.Currency,
                    IsDeleted = a.IsDeleted,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<AccountResponse?> GetAccountByIdAsync(int userId, int accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId && !a.IsDeleted);

            if (account == null) return null;

            return new AccountResponse
            {
                Id = account.Id,
                Name = account.Name,
                Type = account.Type,
                Balance = account.Balance,
                Currency = account.Currency,
                IsDeleted = account.IsDeleted,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            };
        }

        public async Task<bool> UpdateAccountAsync(int userId, int accountId, UpdateAccountRequest request)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId && !a.IsDeleted);

            if (account == null) return false;

            if (request.Name is not null) account.Name = request.Name;
            if (request.Type.HasValue)    account.Type = request.Type.Value;
            if (request.Currency is not null) account.Currency = request.Currency;

            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAccountAsync(int userId, int accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId && !a.IsDeleted);

            if (account == null) return false;

            account.IsDeleted = true;
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}