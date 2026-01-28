using Microsoft.Extensions.Options;
using PancakeBot.Api.Options;

namespace PancakeBot.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    private const string HeaderName = "X-API-KEY";

    private readonly RequestDelegate _next;
    private readonly ApiKeyOptions _options;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IOptions<ApiKeyOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key missing");
            return;
        }

        if (!string.Equals(providedKey, _options.Key, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        await _next(context);
    }
}
