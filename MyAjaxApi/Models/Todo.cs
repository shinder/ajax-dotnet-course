namespace MyAjaxApi.Models;

// Domain Model：對應資料庫 Todos 資料表
public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }   // false = 未完成，true = 已完成
}

// 新增待辦：只需要標題，Id 由資料庫自動產生，IsDone 預設 false
public class CreateTodoDto
{
    public string Title { get; set; } = string.Empty;
}

// 切換完成狀態（PATCH 用）：只更新 IsDone 這一個欄位
public class UpdateTodoDto
{
    public bool IsDone { get; set; }
}
