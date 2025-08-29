
using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;                  
using BudgetForge.Application.Interfaces;   // ICacheService

namespace BudgetForge.Infrastructure.Services
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer mux)
        {
            if (mux == null) throw new ArgumentNullException(nameof(mux));
            _db = mux.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));

            var json = JsonSerializer.Serialize(value);
            if (ttl.HasValue)
                await _db.StringSetAsync(key, json, ttl.Value).ConfigureAwait(false);
            else
                await _db.StringSetAsync(key, json).ConfigureAwait(false);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));

            var val = await _db.StringGetAsync(key).ConfigureAwait(false);
            if (!val.HasValue) return default;

            return JsonSerializer.Deserialize<T>(val!);
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));
            return _db.KeyExistsAsync(key);
        }

        public Task<bool> RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));
            return _db.KeyDeleteAsync(key);
        }
    }
}