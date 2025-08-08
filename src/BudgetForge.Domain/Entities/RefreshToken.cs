using System;

namespace BudgetForge.Domain.Entities
{
    /// <summary>
    /// Represents a refresh token for JWT authentication
    /// Refresh tokens allow users to stay logged in without re-entering credentials
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? RevokedByIp { get; set; }
        public string? CreatedByIp { get; set; }

        // No direct AppUser navigation in Domain

        // Computed properties - calculated, not stored in database
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;

        public RefreshToken()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public RefreshToken(string token, int userId, DateTime expiresAt, string? createdByIp = null) : this()
        {
            Token = token;
            UserId = userId;
            ExpiresAt = expiresAt;
            CreatedByIp = createdByIp;
        }

        public void Revoke(string? revokedByIp = null, string? replacedByToken = null)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevokedByIp = revokedByIp;
            ReplacedByToken = replacedByToken;
        }
    }
}