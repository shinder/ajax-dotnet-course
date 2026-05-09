using Microsoft.AspNetCore.Diagnostics;

namespace MyAjaxApi.Infrastructure;

// IExceptionHandler 是 .NET 8+ 提供的全域例外處理介面。
// 攔截所有未被 try/catch 捕捉的例外，統一回傳 500，
// 避免把 Stack Trace 和內部錯誤細節洩漏給用戶端。
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    // TryHandleAsync：return true 表示已處理，不再往下傳遞
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception exception,
        CancellationToken ct)
    {
        // 把完整錯誤記錄到 Log（伺服器端可查），但不回傳給用戶端
        _logger.LogError(exception, "未預期的錯誤");

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // 回傳簡化的錯誤訊息，不含敏感的 Stack Trace
        await ctx.Response.WriteAsJsonAsync(new
        {
            title = "伺服器發生錯誤，請稍後再試",
            status = 500
        }, ct);

        return true;
    }
}
