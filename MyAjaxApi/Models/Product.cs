using System.ComponentModel.DataAnnotations;

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
//   2. 方便對輸入做驗證（搭配 Data Annotations 或 FluentValidation）
//   3. 讓 API 介面和資料庫結構可以獨立演進

// 新增商品時，前端送進來的 JSON 對應這個類別。
// 屬性上方的 [Required]、[Range] 等是 Data Annotations 驗證標記，
// 配合 [ApiController] 會自動驗證並在失敗時回傳 400。
public class CreateProductDto
{
    [Required(ErrorMessage = "商品名稱為必填")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "名稱長度需在 1 到 100 字之間")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1_000_000, ErrorMessage = "價格需在 0.01 到 1,000,000 之間")]
    public decimal Price { get; set; }

    [Url(ErrorMessage = "請輸入有效的 URL")]
    public string? ImageUrl { get; set; }
}

// 更新商品時，前端送進來的 JSON 對應這個類別
public class UpdateProductDto
{
    [Required(ErrorMessage = "商品名稱為必填")]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1_000_000)]
    public decimal Price { get; set; }

    [Url]
    public string? ImageUrl { get; set; }
}
