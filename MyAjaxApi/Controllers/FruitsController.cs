using Microsoft.AspNetCore.Mvc;
using MyAjaxApi.Models;

namespace MyAjaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FruitsController : ControllerBase
{
    // 暫時用靜態 List 當資料來源（多放幾筆，分頁才看得出效果）。
    // 注意：靜態 List 與 _nextId++ 都不是執行緒安全的，多個請求同時新增可能出錯；
    // 這是教學用的簡化，正式環境要用資料庫（第 7 章）或加鎖。
    // 另外這支直接回傳 Fruit 實體而不是另外定義輸出 DTO，小型唯讀資源這樣寫可以接受，
    // 欄位多、需要隱藏內部資訊時再拆 DTO（見 Models/Product.cs 的說明）。
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

    // 給 NotificationsController 的 SSE 推送用：目前的水果總數
    public static int Count => _fruits.Count;

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
            "id"         => query.OrderBy(f => f.Id),
            "name"       => query.OrderBy(f => f.Name),
            "price"      => query.OrderBy(f => f.Price),
            "price_desc" => query.OrderByDescending(f => f.Price),
            _            => query.OrderBy(f => f.Id),
        };

        // 分頁：防止前端送 page=0 或 size=100000 這種值
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var total = query.Count();

        // 頁碼超過最後一頁就退回最後一頁，避免前端顯示「第 999 / 3 頁」
        var totalPages = Math.Max((int)Math.Ceiling(total / (double)size), 1);
        page = Math.Min(page, totalPages);

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

    // 允許上傳的圖片格式：key 是副檔名，value 是對應的 MIME 類型
    private static readonly Dictionary<string, string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"]  = "image/png",
        [".gif"]  = "image/gif",
        [".webp"] = "image/webp",
    };

    // 上傳：POST /api/fruits/upload（multipart/form-data）→ 200 或 400
    // 對應講義 6-1。IFormFile 參數在 [ApiController] 下會自動從表單讀取。
    // 上傳的檔案會放進 wwwroot/uploads 讓瀏覽器直接開，所以「只收圖片」是安全底線：
    // 若放行 .html 或 .svg，任何人都能上傳一個含 script 的檔案，再用同源的網址讓別人開啟（儲存型 XSS）。
    [HttpPost("upload")]
    [RequestSizeLimit(2 * 1024 * 1024)]   // 整個請求最多 2 MB，超過時讀取表單會失敗，[ApiController] 回 400 並在 errors 說明
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string note,
        [FromServices] IWebHostEnvironment env)   // 用 DI 取得 wwwroot 的實際路徑，不要用 Directory.GetCurrentDirectory()
    {
        if (file is null || file.Length == 0)
            ModelState.AddModelError("file", "沒有收到檔案");

        // 副檔名與 MIME 類型都要對，而且副檔名由伺服器決定（不信任用戶端的檔名）
        var ext = Path.GetExtension(file?.FileName ?? "");
        if (!AllowedImageTypes.TryGetValue(ext, out var expectedMime))
            ModelState.AddModelError("file", "只接受 jpg、png、gif、webp 圖片");
        else if (!string.Equals(file!.ContentType, expectedMime, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError("file", $"檔案內容類型 {file.ContentType} 與副檔名不符");

        // 回 400 + ValidationProblemDetails，格式與其他驗證錯誤一致，前端的 api.js 才能統一處理
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);

        // 隨機檔名 + 白名單裡的副檔名（轉小寫），避免覆蓋、路徑穿越與奇怪的大小寫
        // System.IO.File 要寫全名，因為 ControllerBase 也有 File() 方法
        var savedName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(dir, savedName);

        await using var stream = System.IO.File.Create(fullPath);
        await file!.CopyToAsync(stream);

        // wwwroot 由 UseStaticFiles 提供，回傳的 url 可直接在瀏覽器開啟
        return Ok(new { url = $"/uploads/{savedName}", size = file.Length, note });
    }
}
