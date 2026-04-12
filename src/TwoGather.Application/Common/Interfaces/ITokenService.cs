using System.Security.Claims;
using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
