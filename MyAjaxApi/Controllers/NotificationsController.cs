using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MyAjaxApi.Controllers;

// Server-Sent Events（SSE）範例，對應講義 6-2。
// 一條 HTTP 連線不關閉，伺服器每 2 秒推一筆資料，瀏覽器用 EventSource 接收。
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    // GET /api/notifications/stream
    // 回傳型別是 Task 而不是 IActionResult：我們自己寫 Response，框架不要再幫忙包裝
    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        // SSE 的三個必要條件：Content-Type 是 text/event-stream、不要快取、不要緩衝
        // 這些 Header 一定要在第一次寫入 Body 之前設定
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        try
        {
            // ct 會在瀏覽器關閉連線（source.close() 或關掉分頁）時被取消，迴圈就會結束
            for (var i = 1; !ct.IsCancellationRequested; i++)
            {
                var payload = new
                {
                    seq = i,
                    time = DateTime.UtcNow,
                    fruitCount = FruitsController.Count,
                };

                // SSE 的格式：每則訊息是一行以 "data: " 開頭的文字，用「空一行」結尾
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n", ct);
                await Response.Body.FlushAsync(ct);   // 立刻送出，不要卡在緩衝區

                await Task.Delay(2000, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // 用戶端斷線時 Task.Delay / WriteAsync 會拋這個例外，這是正常結束，不是錯誤；
            // 不接住的話會進到全域例外處理，在 log 留下一筆假的 500。
        }
    }
}
