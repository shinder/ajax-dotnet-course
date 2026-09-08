using Microsoft.AspNetCore.Mvc;
using MyAjaxApi.Models;

namespace MyAjaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FruitsController : ControllerBase
{
    // 暫時用靜態 List 當資料來源
    private static readonly List<Fruit> _fruits = new()
    {
        new Fruit { Id = 1, Name = "蘋果", Price = 30 },
        new Fruit { Id = 2, Name = "香蕉", Price = 15 },
    };
    private static int _nextId = 3;

    // 取得全部：GET /api/fruits → 200
    [HttpGet]
    public ActionResult<IEnumerable<Fruit>> GetAll() => Ok(_fruits);

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
