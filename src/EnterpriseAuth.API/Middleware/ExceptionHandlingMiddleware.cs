using EnterpriseAuth.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace EnterpriseAuth.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ProblemDetails();

            switch (exception)
            {
                case ValidationException validationEx:
                    response.Title = "Validation Error";
                    response.Status = (int)HttpStatusCode.BadRequest;
                    response.Detail = string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage));
                    response.Extensions["errors"] = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case InvalidCredentialsException:
                    response.Title = "Authentication Failed";
                    response.Status = (int)HttpStatusCode.Unauthorized;
                    response.Detail = "Invalid username or password.";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case UserNotFoundException userNotFoundEx:
                    response.Title = "User Not Found";
                    response.Status = (int)HttpStatusCode.NotFound;
                    response.Detail = userNotFoundEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case UserLockedOutException userLockedEx:
                    response.Title = "Account Locked";
                    response.Status = (int)HttpStatusCode.Locked;
                    response.Detail = userLockedEx.Message;
                    response.Extensions["lockedUntil"] = userLockedEx.LockedUntil;
                    context.Response.StatusCode = (int)HttpStatusCode.Locked;
                    break;

                case DomainException domainEx:
                    response.Title = "Domain Error";
                    response.Status = (int)HttpStatusCode.BadRequest;
                    response.Detail = domainEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case UnauthorizedAccessException:
                    response.Title = "Unauthorized";
                    response.Status = (int)HttpStatusCode.Unauthorized;
                    response.Detail = "You are not authorized to perform this action.";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case ArgumentException argumentEx:
                    response.Title = "Invalid Argument";
                    response.Status = (int)HttpStatusCode.BadRequest;
                    response.Detail = argumentEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case InvalidOperationException invalidOpEx:
                    response.Title = "Invalid Operation";
                    response.Status = (int)HttpStatusCode.BadRequest;
                    response.Detail = invalidOpEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                default:
                    response.Title = "Internal Server Error";
                    response.Status = (int)HttpStatusCode.InternalServerError;
                    response.Detail = "An error occurred while processing your request.";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ProblemDetails
    {
        public string Title { get; set; } = string.Empty;
        public int? Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Instance { get; set; }
        public Dictionary<string, object> Extensions { get; set; } = new();
    }
}