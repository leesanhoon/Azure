using EnterpriseAuth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace EnterpriseAuth.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateJwtToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
        RefreshToken GenerateRefreshToken(string ipAddress);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
        DateTime GetTokenExpiration(string token);
    }
}