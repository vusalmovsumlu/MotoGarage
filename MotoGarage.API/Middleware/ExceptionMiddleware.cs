using MotoGarage.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace MotoGarage.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ForbiddenException ex)
        {
            await WriteErrorResponseAsync(context, HttpStatusCode.Forbidden, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteErrorResponseAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Domain exception: {Message}",
                ex.Message);

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.BadRequest,
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "Unauthorized request: {Message}",
                ex.Message);

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.Unauthorized,
                ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                "Resource not found: {Message}",
                ex.Message);

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.NotFound,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Server xətası baş verdi.");
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message)
    {
        context.Response.StatusCode =
            (int)statusCode;

        context.Response.ContentType =
            "application/json";

        var response = new
        {
            statusCode = (int)statusCode,
            message
        };

        var json =
            JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}