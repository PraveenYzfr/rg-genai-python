using System.Net;
using System.Text.Json;
using GenAI.Domain.Exceptions;

namespace GenAI.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception occurred.");
            await WriteErrorResponseAsync(context, exception);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, "Domain error"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            title,
            status = (int)statusCode,
            detail = exception.Message
        });

        await context.Response.WriteAsync(payload);
    }
}
