using Microsoft.AspNetCore.Diagnostics;

namespace MyAjaxApi.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "未預期的錯誤");

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new
        {
            title = "伺服器發生錯誤，請稍後再試",
            status = 500
        }, ct);

        return true;
    }
}
