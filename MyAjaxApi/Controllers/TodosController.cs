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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Todo>>> GetAll()
    {
        var todos = await _db.Todos.OrderBy(t => t.Id).ToListAsync();
        return Ok(todos);
    }

    [HttpPost]
    public async Task<ActionResult<Todo>> Create([FromBody] CreateTodoDto dto)
    {
        var todo = new Todo { Title = dto.Title };
        _db.Todos.Add(todo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), todo);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Toggle(int id, [FromBody] UpdateTodoDto dto)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo is null) return NotFound();

        todo.IsDone = dto.IsDone;
        await _db.SaveChangesAsync();

        return NoContent();
    }

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
