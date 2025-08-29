namespace BudgetForge.Application.Interfaces
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task<T?> GetAsync<T>(string key);
        Task<bool> ExistsAsync(string key);
        Task<bool> RemoveAsync(string key);
    }
}