using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameShelf.Web.Filters;

/// return JSON error responses for API controllers when exception is thrown
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly IHostEnvironment _env;

    public ApiExceptionFilter(IHostEnvironment env)
    {
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api"))
            return;

        var (statusCode, message) = context.Exception switch
        {
            KeyNotFoundException => (404, context.Exception.Message),
            ArgumentException => (400, context.Exception.Message),
            UnauthorizedAccessException => (401, "Unauthorized."),
            _ => (500, _env.IsDevelopment() ? context.Exception.Message : "An error occurred.")
        };

        context.Result = new ObjectResult(new { error = message, statusCode })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}

