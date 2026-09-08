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
}
