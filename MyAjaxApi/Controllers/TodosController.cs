using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAjaxApi.Data;
using MyAjaxApi.Models;

namespace MyAjaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly AppDbContext _db;

    public TodosController(AppDbContext db) => _db = db;

    // GET /api/todos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Todo>>> GetAll()
    {
        // 依 Id 升序排列，讓新增的待辦顯示在最後
        var todos = await _db.Todos.OrderBy(t => t.Id).ToListAsync();
        return Ok(todos);
    }

    // POST /api/todos
    // Body: { "title": "買菜" }
    [HttpPost]
    public async Task<ActionResult<Todo>> Create(CreateTodoDto dto)
    {
        // IsDone 不需要設定，C# bool 預設值是 false
        var todo = new Todo { Title = dto.Title };
        _db.Todos.Add(todo);
        await _db.SaveChangesAsync();

        // 201 Created，回傳新建的 todo（含自動產生的 Id）
        return CreatedAtAction(nameof(GetAll), todo);
    }

    // PATCH /api/todos/5
    // 部分更新：只更新 IsDone，不動 Title
    // 使用 PATCH 而非 PUT，因為只更新單一欄位
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Toggle(int id, UpdateTodoDto dto)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo is null) return NotFound();

        todo.IsDone = dto.IsDone;
        await _db.SaveChangesAsync();

        return NoContent();   // 204
    }

    // DELETE /api/todos/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo is null) return NotFound();

        _db.Todos.Remove(todo);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
