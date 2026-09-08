using Microsoft.AspNetCore.Mvc;

namespace MyAjaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasicsController : ControllerBase
{
    // GET /api/basics
    // 回傳匿名物件，會序列化成 JSON：{"message":"Hello API"}
    // 若直接 Ok("Hello API")，回應會是 text/plain 純字串，前端用 r.json() 會解析失敗
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "Hello API" });
}
