using BCrypt.Net;
using Microsoft.Extensions.Logging;
using EnterpriseAuth.Application.DTOs;
using EnterpriseAuth.Application.Interfaces;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Exceptions;
using EnterpriseAuth.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnterpriseAuth.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
        {
            _logger.LogInformation("Login attempt for user: {UsernameOrEmail}", request.UsernameOrEmail);

            var user = await _unitOfWork.Users.GetUserWithRolesAndPermissionsAsync(
                await GetUserIdByUsernameOrEmailAsync(request.UsernameOrEmail));

            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {UsernameOrEmail}", request.UsernameOrEmail);
                throw new InvalidCredentialsException();
            }

            // Check if account is locked
            if (user.IsLockedOut())
            {
                _logger.LogWarning("Login failed - account locked: {UserId}", user.Id);
                throw new UserLockedOutException(user.LockedOutUntil!.Value);
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed - account inactive: {UserId}", user.Id);
                throw new InvalidCredentialsException("Account is inactive.");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.IncrementFailedLoginAttempts();
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning("Login failed - invalid password: {UserId}, Failed attempts: {FailedAttempts}",
                    user.Id, user.FailedLoginAttempts);

                throw new InvalidCredentialsException();
            }

            // Successful login
            user.UpdateLastLogin();
            await _unitOfWork.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList();

            var accessToken = _jwtService.GenerateJwtToken(user, roles, permissions);
            var refreshToken = _jwtService.GenerateRefreshToken(ipAddress);

            refreshToken.UserId = user.Id;
            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Login successful for user: {UserId}", user.Id);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = _jwtService.GetTokenExpiration(accessToken),
                User = MapToUserDto(user, roles, permissions)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            _logger.LogInformation("Registration attempt for user: {Username}, Email: {Email}",
                request.Username, request.Email);

            // Check if username exists
            if (await _unitOfWork.Users.IsUsernameExistsAsync(request.Username))
            {
                _logger.LogWarning("Registration failed - username exists: {Username}", request.Username);
                throw new InvalidOperationException("Username already exists.");
            }

            // Check if email exists
            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
            {
                _logger.LogWarning("Registration failed - email exists: {Email}", request.Email);
                throw new InvalidOperationException("Email already exists.");
            }

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                IsEmailConfirmed = false
            };

            await _unitOfWork.Users.AddAsync(user);

            // Assign default role
            var defaultRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.IsDefault);
            if (defaultRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id
                };
                await _unitOfWork.UserRoles.AddAsync(userRole);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Registration successful for user: {UserId}", user.Id);

            // Get user with roles for response
            var userWithRoles = await _unitOfWork.Users.GetUserWithRolesAndPermissionsAsync(user.Id);
            var roles = userWithRoles?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
            var permissions = userWithRoles?.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList() ?? new List<string>();

            var accessToken = _jwtService.GenerateJwtToken(user, roles, permissions);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = string.Empty, // No refresh token on registration
                ExpiresAt = _jwtService.GetTokenExpiration(accessToken),
                User = MapToUserDto(user, roles, permissions)
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            _logger.LogInformation("Refresh token attempt for token: {TokenPrefix}...",
                refreshToken.Length > 10 ? refreshToken[..10] : refreshToken);

            var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

            if (token == null || !token.IsActive)
            {
                _logger.LogWarning("Refresh token failed - invalid or inactive token");
                throw new InvalidOperationException("Invalid refresh token.");
            }

            var user = await _unitOfWork.Users.GetUserWithRolesAndPermissionsAsync(token.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token failed - user not found or inactive: {UserId}", token.UserId);
                throw new InvalidOperationException("User not found or inactive.");
            }

            // Revoke old token
            token.Revoke(ipAddress);

            // Generate new tokens
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList();

            var accessToken = _jwtService.GenerateJwtToken(user, roles, permissions);
            var newRefreshToken = _jwtService.GenerateRefreshToken(ipAddress);

            newRefreshToken.UserId = user.Id;
            token.ReplacedByToken = newRefreshToken.Token;

            await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Refresh token successful for user: {UserId}", user.Id);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = _jwtService.GetTokenExpiration(accessToken),
                User = MapToUserDto(user, roles, permissions)
            };
        }

        public async Task RevokeTokenAsync(string? refreshToken, string ipAddress)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentException("Token is required.", nameof(refreshToken));
            }

            _logger.LogInformation("Revoke token attempt for token: {TokenPrefix}...",
                refreshToken.Length > 10 ? refreshToken[..10] : refreshToken);

            var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

            if (token == null || !token.IsActive)
            {
                _logger.LogWarning("Revoke token failed - token not found or already revoked");
                throw new InvalidOperationException("Token not found or already revoked.");
            }

            token.Revoke(ipAddress);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Token revoked successfully for user: {UserId}", token.UserId);
        }

        public async Task RevokeAllUserTokensAsync(string usernameOrEmail)
        {
            _logger.LogInformation("Revoke all tokens attempt for user: {UsernameOrEmail}", usernameOrEmail);

            var userId = await GetUserIdByUsernameOrEmailAsync(usernameOrEmail);
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("All tokens revoked for user: {UserId}", userId);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return await Task.FromResult(_jwtService.ValidateToken(token));
        }

        private async Task<Guid> GetUserIdByUsernameOrEmailAsync(string usernameOrEmail)
        {
            var user = await _unitOfWork.Users.GetByUsernameOrEmailAsync(usernameOrEmail);
            if (user == null)
            {
                throw new UserNotFoundException(usernameOrEmail);
            }
            return user.Id;
        }

        private static UserDto MapToUserDto(User user, List<string> roles, List<string> permissions)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                Roles = roles,
                Permissions = permissions
            };
        }
    }
}