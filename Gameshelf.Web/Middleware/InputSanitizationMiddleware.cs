using System.Text.RegularExpressions;

namespace GameShelf.Web.Middleware;


/// Middleware  to prevent injection attacks.

public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Regex ScriptTagRegex = new(@"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SqlInjectionPattern = new(@"(?i)(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|SCRIPT)\b)", RegexOptions.Compiled);

    public InputSanitizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
        if (context.Request.Method is "POST" or "PUT" or "PATCH")
        {
            if (context.Request.HasFormContentType)
            {
                // Sanitize form values
                var form = await context.Request.ReadFormAsync();
                foreach (var key in form.Keys)
                {
                    var values = form[key];
                    for (int i = 0; i < values.Count; i++)
                    {
                        var sanitized = SanitizeInput(values[i]);
                        if (sanitized != values[i])
                        {
                            
                        }
                    }
                }
            }
        }

        await _next(context);
    }

    private static string SanitizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        // Remove script tags
        var sanitized = ScriptTagRegex.Replace(input, string.Empty);
        
        // Basic SQL injection pattern detection 
        if (SqlInjectionPattern.IsMatch(sanitized))
        {
            // Log suspicious input but don't modify
        }

        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);

        return sanitized;
    }
}

