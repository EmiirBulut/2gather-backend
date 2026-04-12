using System.Text.Json;
using FluentValidation;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, error, details) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validationEx.Errors.Select(e => e.ErrorMessage).ToArray()
            ),
            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                notFoundEx.Message,
                Array.Empty<string>()
            ),
            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                forbiddenEx.Message,
                Array.Empty<string>()
            ),
            DomainException domainEx => (
                StatusCodes.Status422UnprocessableEntity,
                domainEx.Message,
                Array.Empty<string>()
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                Array.Empty<string>()
            )
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            status = statusCode,
            error,
            details
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
