using BudgetForge.Application.DTOs;

namespace BudgetForge.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransactionAsync(int userId, CreateTransactionRequest request);
        Task<IEnumerable<TransactionResponse>> GetTransactionsAsync(int userId);
        Task<TransactionResponse?> GetTransactionByIdAsync(int userId, int transactionId);
        Task<IEnumerable<TransactionResponse>> GetTransactionsByAccountAsync(int userId, int accountId);
        Task<bool> UpdateTransactionAsync(int userId, int transactionId, UpdateTransactionRequest request);
        Task<bool> DeleteTransactionAsync(int userId, int transactionId);
    }
}