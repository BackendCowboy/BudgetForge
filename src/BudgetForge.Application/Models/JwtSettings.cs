namespace BudgetForge.Application.Models;

public sealed class JwtSettings
{
    public string SecretKey { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    public int AccessTokenExpiryMinutes { get; set; } = 120;
    public int RefreshTokenExpiryDays { get; set; } = 7;

    public bool RequireHttpsMetadata { get; set; } = false;
    public bool SaveToken { get; set; } = true;

    public bool ValidateIssuerSigningKey { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;

    public int ClockSkewSeconds { get; set; } = 0;
}