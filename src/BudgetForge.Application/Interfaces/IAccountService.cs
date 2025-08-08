using BudgetForge.Application.DTOs;

namespace BudgetForge.Application.Interfaces
{
    public interface IAccountService
    {
        Task<AccountResponse> CreateAccountAsync(int userId, CreateAccountRequest request);
        Task<IEnumerable<AccountResponse>> GetUserAccountsAsync(int userId);
        Task<AccountResponse?> GetAccountByIdAsync(int userId, int accountId);
        Task<bool> UpdateAccountAsync(int userId, int accountId, UpdateAccountRequest request);
        Task<bool> SoftDeleteAccountAsync(int userId, int accountId);
    }
}