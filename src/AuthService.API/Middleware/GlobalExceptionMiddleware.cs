using System.Net;
using System.Text.Json;
using AuthService.API.Models;
using AuthService.Application.Exceptions;

namespace AuthService.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception occurred");

                var response = context.Response;
                response.ContentType = "application/json";

                ApiResponse<string> errorResponse;

                switch (ex)
                {
                    case ValidationException ve:
                        errorResponse = ApiResponse<string>.Fail(
                            message: ve.Message,
                            code: "VALIDATION_ERROR",
                            statusCode: (int)HttpStatusCode.BadRequest
                        );
                        errorResponse.Error!.ValidationErrors = ve.Errors;
                        break;

                    case NotFoundException nf:
                        errorResponse = ApiResponse<string>.Fail(
                            message: nf.Message,
                            code: "NOT_FOUND",
                            statusCode: (int)HttpStatusCode.NotFound
                        );
                        break;

                    case UnauthorizedAccessException:
                        errorResponse = ApiResponse<string>.Fail(
                            message: "Unauthorized",
                            code: "UNAUTHORIZED",
                            statusCode: (int)HttpStatusCode.Unauthorized
                        );
                        break;

                    default:
                        errorResponse = ApiResponse<string>.Fail(
                            message: "An unexpected error occurred. Please try again later.",
                            code: "SERVER_ERROR",
                            statusCode: (int)HttpStatusCode.InternalServerError
                        );
                        break;
                }

                response.StatusCode = errorResponse.Error?.StatusCode
                                      ?? (int)HttpStatusCode.InternalServerError;

                var json = JsonSerializer.Serialize(errorResponse);
                await response.WriteAsync(json);
            }
        }
    }
}
