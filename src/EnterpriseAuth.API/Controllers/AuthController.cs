using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseAuth.Application.DTOs;
using EnterpriseAuth.Application.Interfaces;
using EnterpriseAuth.Domain.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace EnterpriseAuth.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Locked)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var response = await _authService.LoginAsync(request, ipAddress);

                _logger.LogInformation("User {Username} logged in successfully from IP {IpAddress}",
                    request.UsernameOrEmail, ipAddress);

                return Ok(response);
            }
            catch (InvalidCredentialsException ex)
            {
                _logger.LogWarning("Invalid login attempt for {Username}: {Message}",
                    request.UsernameOrEmail, ex.Message);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = ex.Message,
                    Status = (int)HttpStatusCode.Unauthorized
                });
            }
            catch (UserLockedOutException ex)
            {
                _logger.LogWarning("Login attempt for locked account {Username}: {Message}",
                    request.UsernameOrEmail, ex.Message);
                return StatusCode((int)HttpStatusCode.Locked, new ProblemDetails
                {
                    Title = "Account Locked",
                    Detail = ex.Message,
                    Status = (int)HttpStatusCode.Locked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Username}", request.UsernameOrEmail);
                return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request.",
                    Status = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);

                _logger.LogInformation("User {Username} registered successfully", request.Username);

                return CreatedAtAction(nameof(Register), response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration failed for {Username}: {Message}",
                    request.Username, ex.Message);
                return Conflict(new ProblemDetails
                {
                    Title = "Registration Failed",
                    Detail = ex.Message,
                    Status = (int)HttpStatusCode.Conflict
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Username}", request.Username);
                return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request.",
                    Status = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        /// <summary>
        /// Refresh an expired JWT token using a refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New authentication response with JWT token</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var response = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

                _logger.LogInformation("Token refreshed successfully from IP {IpAddress}", ipAddress);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Refresh Failed",
                    Detail = ex.Message,
                    Status = (int)HttpStatusCode.Unauthorized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request.",
                    Status = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        /// <summary>
        /// Revoke a refresh token
        /// </summary>
        /// <param name="request">Token revocation request</param>
        /// <returns>Success response</returns>
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);

                _logger.LogInformation("Token revoked successfully from IP {IpAddress}", ipAddress);

                return Ok(new { message = "Token revoked successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = ex.Message,
                    Status = (int)HttpStatusCode.BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Token Revocation Failed",
                    Detail = ex.Message,
                    Status = (int)HttpStatusCode.BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token revocation");
                return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request.",
                    Status = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        /// <summary>
        /// Logout current user (revoke all refresh tokens)
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var usernameOrEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(usernameOrEmail))
                {
                    return Unauthorized(new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Detail = "User identity not found.",
                        Status = (int)HttpStatusCode.Unauthorized
                    });
                }

                await _authService.RevokeAllUserTokensAsync(usernameOrEmail);

                _logger.LogInformation("User {Username} logged out successfully", usernameOrEmail);

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request.",
                    Status = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var user = new
                {
                    Id = User.FindFirst("user_id")?.Value,
                    Username = User.FindFirst("username")?.Value,
                    Email = User.FindFirst("email")?.Value,
                    IsActive = User.FindFirst("is_active")?.Value,
                    IsEmailConfirmed = User.FindFirst("is_email_confirmed")?.Value,
                    Roles = User.FindAll("role").Select(c => c.Value).ToArray(),
                    Permissions = User.FindAll("permission").Select(c => c.Value).ToArray()
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user information");
                return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request.",
                    Status = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        private string GetIpAddress()
        {
            // Check for X-Forwarded-For header (for load balancers/proxies)
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ?? "Unknown";
            }

            // Check for X-Real-IP header (for nginx)
            if (Request.Headers.ContainsKey("X-Real-IP"))
            {
                return Request.Headers["X-Real-IP"].FirstOrDefault() ?? "Unknown";
            }

            // Fall back to remote IP address
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}