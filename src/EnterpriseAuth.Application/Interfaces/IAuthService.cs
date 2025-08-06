using EnterpriseAuth.Application.DTOs;
using System.Threading.Tasks;

namespace EnterpriseAuth.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task RevokeTokenAsync(string? refreshToken, string ipAddress);
        Task RevokeAllUserTokensAsync(string usernameOrEmail);
        Task<bool> ValidateTokenAsync(string token);
    }
}