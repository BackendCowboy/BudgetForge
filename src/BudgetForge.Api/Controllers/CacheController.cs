using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace BudgetForge.Api.Controllers
{
    [ApiController]
    [Route("cache")]
    [Produces("application/json")]
    public sealed class CacheController : ControllerBase
    {
        private readonly IDatabase _db;

        public CacheController(IConnectionMultiplexer mux)
        {
            _db = mux.GetDatabase();
        }

        // ---------- DTO for flexible input ----------
        public sealed class CacheSetDto
        {
            public string? Value { get; set; }
            public int? TtlSeconds { get; set; }
        }

        // ---------- GET /cache/{key} ----------
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest(new { message = "key required" });

            var val = await _db.StringGetAsync(key);
            if (val.IsNull)
                return NotFound(new { key, message = "not found" });

            var ttl = await _db.KeyTimeToLiveAsync(key);
            return Ok(new
            {
                key,
                value = val.ToString(),
                ttlSeconds = ttl?.TotalSeconds
            });
        }

        // ---------- PUT /cache/{key} ----------
        // Accepts either:
        //   - application/json: { "value": "...", "ttlSeconds": 120 }
        //   - text/plain body with ?ttlSeconds=120
        //   - query: ?value=...&ttlSeconds=...
        [HttpPut("{key}")]
        [Consumes("application/json", "text/plain")]
        public async Task<IActionResult> Put(
            string key,
            [FromBody] CacheSetDto? body,       // when Content-Type: application/json
            [FromQuery] string? value,          // fallback if provided via query
            [FromQuery] int? ttlSeconds = null) // fallback if provided via query
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest(new { message = "key required" });

            var val = body?.Value ?? value;

            // If Content-Type: text/plain, MVC doesn't bind the raw body to body.Value automatically.
            // For that case, try reading the raw body if val is still null.
            if (val is null && Request.ContentType?.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                var raw = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(raw))
                    val = raw;
            }

            var ttl = body?.TtlSeconds ?? ttlSeconds;

            if (string.IsNullOrWhiteSpace(val))
                return BadRequest(new { message = "value required" });

            TimeSpan? expiry = ttl is > 0 ? TimeSpan.FromSeconds(ttl.Value) : null;
            await _db.StringSetAsync(key, val, expiry);

            return Ok(new
            {
                key,
                set = true,
                expiresInSeconds = ttl ?? 0
            });
        }

        // ---------- DELETE /cache/{key} ----------
        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest(new { message = "key required" });

            var removed = await _db.KeyDeleteAsync(key);
            return Ok(new { key, removed });
        }
    }
}