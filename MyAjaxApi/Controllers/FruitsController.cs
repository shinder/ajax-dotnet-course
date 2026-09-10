using Microsoft.AspNetCore.Mvc;
using MyAjaxApi.Models;

namespace MyAjaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FruitsController : ControllerBase
{
    // 暫時用靜態 List 當資料來源（多放幾筆，分頁才看得出效果）
    private static readonly List<Fruit> _fruits = new()
    {
        new Fruit { Id = 1,  Name = "蘋果",   Price = 30 },
        new Fruit { Id = 2,  Name = "香蕉",   Price = 15 },
        new Fruit { Id = 3,  Name = "芒果",   Price = 50 },
        new Fruit { Id = 4,  Name = "鳳梨",   Price = 45 },
        new Fruit { Id = 5,  Name = "西瓜",   Price = 120 },
        new Fruit { Id = 6,  Name = "葡萄",   Price = 80 },
        new Fruit { Id = 7,  Name = "草莓",   Price = 150 },
        new Fruit { Id = 8,  Name = "芭樂",   Price = 25 },
        new Fruit { Id = 9,  Name = "木瓜",   Price = 35 },
        new Fruit { Id = 10, Name = "荔枝",   Price = 90 },
        new Fruit { Id = 11, Name = "柳丁",   Price = 20 },
        new Fruit { Id = 12, Name = "奇異果", Price = 40 },
    };
    private static int _nextId = 13;

    // 取得列表：GET /api/fruits?name=果&sort=price_desc&page=1&size=5 → 200
    // 搜尋、排序、分頁都是「條件」而不是「資源」，一律放查詢字串（講義 1-4、5-8）。
    // 簡單型別參數在 [ApiController] 下預設從查詢字串取，[FromQuery] 可省略，這裡標出來是為了清楚。
    [HttpGet]
    public ActionResult<PagedResult<Fruit>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int size = 5)
    {
        IEnumerable<Fruit> query = _fruits;

        // 篩選：名稱包含關鍵字（不分大小寫）
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(f => f.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        // 排序：只接受白名單裡的值，其他一律回到預設
        query = sort switch
        {
            "name"       => query.OrderBy(f => f.Name),
            "price"      => query.OrderBy(f => f.Price),
            "price_desc" => query.OrderByDescending(f => f.Price),
            _            => query.OrderBy(f => f.Id),
        };

        // 分頁：防止前端送 page=0 或 size=100000 這種值
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var total = query.Count();
        var items = query.Skip((page - 1) * size).Take(size).ToList();

        return Ok(new PagedResult<Fruit> { Items = items, Total = total, Page = page, Size = size });
    }

    // 慢速回應：GET /api/fruits/slow?seconds=10 → 等待指定秒數後才回傳全部水果
    // 教學用：用同步的 XHR（xhr.open(..., false)）呼叫這支，可以看到整個頁面在等待期間完全卡死；
    // 改用非同步呼叫則頁面照常可以操作，這就是 AJAX 的 A（Asynchronous）存在的理由。
    // 路由要放在 {id:int} 之前沒關係，因為 "slow" 不是整數，不會被 GetById 搶走。
    [HttpGet("slow")]
    public async Task<ActionResult<object>> GetSlow([FromQuery] int seconds = 10, CancellationToken ct = default)
    {
        seconds = Math.Clamp(seconds, 1, 30);

        // 傳入 CancellationToken：使用者關閉頁面或取消請求時，伺服器不會繼續空等
        await Task.Delay(TimeSpan.FromSeconds(seconds), ct);

        return Ok(new { waitedSeconds = seconds, items = _fruits });
    }

    // 取得單筆：GET /api/fruits/5 → 200 或 404
    [HttpGet("{id:int}")]
    public ActionResult<Fruit> GetById(int id)
    {
        var fruit = _fruits.FirstOrDefault(p => p.Id == id);
        if (fruit is null) return NotFound();
        return Ok(fruit);
    }

    // 新增：POST /api/fruits → 201 Created（附 Location）
    [HttpPost]
    public ActionResult<Fruit> Create(CreateFruitDto dto)
    {
        var fruit = new Fruit { Id = _nextId++, Name = dto.Name, Price = dto.Price };
        _fruits.Add(fruit);
        return CreatedAtAction(nameof(GetById), new { id = fruit.Id }, fruit);
    }

    // 更新：PUT /api/fruits/5 → 204 No Content 或 404
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, UpdateFruitDto dto)
    {
        var fruit = _fruits.FirstOrDefault(p => p.Id == id);
        if (fruit is null) return NotFound();
        fruit.Name = dto.Name;
        fruit.Price = dto.Price;
        return NoContent();
    }

    // 刪除：DELETE /api/fruits/5 → 204 No Content 或 404
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var fruit = _fruits.FirstOrDefault(p => p.Id == id);
        if (fruit is null) return NotFound();
        _fruits.Remove(fruit);
        return NoContent();
    }

    // 上傳：POST /api/fruits/upload（multipart/form-data）→ 200 或 400
    // 對應講義 6-1。IFormFile 參數在 [ApiController] 下會自動從表單讀取。
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string note)
    {
        if (file is null || file.Length == 0)
            return BadRequest("沒有收到檔案");

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(dir);

        // 用隨機檔名儲存，避免覆蓋與路徑穿越；System.IO.File 要寫全名，因為 ControllerBase 也有 File() 方法
        var savedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(dir, savedName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        // wwwroot 由 UseStaticFiles 提供，回傳的 url 可直接在瀏覽器開啟
        return Ok(new { url = $"/uploads/{savedName}", size = file.Length, note });
    }
}
