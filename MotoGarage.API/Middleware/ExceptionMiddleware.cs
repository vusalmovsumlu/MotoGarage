using System.Net;
using System.Text.Json;
using MotoGarage.Domain.Exceptions;

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

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred."
        );

        var statusCode = exception switch
        {
            DomainException =>
                HttpStatusCode.BadRequest,

            UnauthorizedAccessException =>
                HttpStatusCode.Unauthorized,

            KeyNotFoundException =>
                HttpStatusCode.NotFound,

            _ =>
                HttpStatusCode.InternalServerError
        };

        var message = statusCode ==
                      HttpStatusCode.InternalServerError
            ? "Serverdə gözlənilməz xəta baş verdi."
            : exception.Message;

        var response = new
        {
            statusCode = (int)statusCode,
            message
        };

        context.Response.ContentType =
            "application/json";

        context.Response.StatusCode =
            (int)statusCode;

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}