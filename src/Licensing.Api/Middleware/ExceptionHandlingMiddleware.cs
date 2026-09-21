using Licensing.Application.Common;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Middleware;

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
        var (status, message, code) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, "not_found"),
            ValidationException => (HttpStatusCode.BadRequest, exception.Message, "validation_error"),
            ConflictException => (HttpStatusCode.Conflict, exception.Message, "conflict"),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message, "forbidden"),
            Licensing.Application.Common.ApplicationException => (HttpStatusCode.BadRequest, exception.Message, "application_error"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", "internal_error")
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = message,
            Type = $"https://httpstatuses.com/{(int)status}",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
