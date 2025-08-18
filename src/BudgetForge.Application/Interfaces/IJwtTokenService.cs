using System.Security.Claims;   

namespace BudgetForge.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(int userId, string email, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token); // nullable return
    }
}