namespace MyAjaxApi.Models;

// Domain Model：對應資料庫 Products 資料表的每一列。
// EF Core 會依據屬性名稱和型別自動對應欄位：
//   - 名為 Id 的 int 屬性 → 自動設為主鍵（Primary Key）並 AUTO INCREMENT
//   - string 屬性 → TEXT 欄位
//   - decimal 屬性 → TEXT 欄位（SQLite 沒有 DECIMAL，EF Core 自動轉換）
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }   // 可選欄位
    public DateTime CreatedAt { get; set; }
}

// DTO（Data Transfer Object）：定義 API 接收的資料形狀。
// 與 Domain Model 分開的原因：
//   1. 避免用戶端傳入不該由外部設定的欄位（例如 Id、CreatedAt）
//   2. 方便對輸入做驗證（搭配 FluentValidation）
//   3. 讓 API 介面和資料庫結構可以獨立演進
//
// 本範例採 FluentValidation，驗證規則寫在 Validators/ 目錄。
// DTO 保持單純資料結構，不放驗證屬性。

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}
