using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace BFF.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (BadHttpRequestException exception)
        {
            logger.LogWarning("Rejected invalid request at {Path}: {Message}", context.Request.Path, exception.Message);
            await WriteProblemAsync(context, exception.StatusCode, "Invalid request.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning("Required BFF configuration is unavailable for {Path}: {Message}", context.Request.Path, exception.Message);
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "A required integration is not configured.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Upstream communication failed for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status502BadGateway, "The upstream service is unavailable.");
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Session storage is unavailable for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "Session storage is unavailable.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled BFF error for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = statusCode, Title = detail });
    }
}
