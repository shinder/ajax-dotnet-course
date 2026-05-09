namespace MyAjaxApi.Models;

public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}

public class CreateTodoDto
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateTodoDto
{
    public bool IsDone { get; set; }
}
