namespace MyAjaxApi.Models;

// Model：內部使用，對應資料庫
public class Fruit
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

// DTO：前端新增 / 更新時送進來的形狀（沒有 Id）
public class CreateFruitDto
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class UpdateFruitDto
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
