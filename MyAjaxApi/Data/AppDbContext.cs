using Microsoft.EntityFrameworkCore;
using MyAjaxApi.Models;

namespace MyAjaxApi.Data;

// DbContext 是 EF Core 的核心類別，代表「一次資料庫工作階段」。
// 每個 HTTP 請求會建立一個新的 AppDbContext（Scoped 生命週期），
// 請求結束後自動釋放，確保連線不會洩漏。
public class AppDbContext : DbContext
{
    // 建構式：把設定（連線字串等）交給父類別處理
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet<T> 對應資料庫的一張資料表。
    // Set<Product>() 與 DbSet<Product> Products { get; set; } 效果相同，
    // 使用 => 語法讓屬性不可被外部設定。
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Todo> Todos => Set<Todo>();
}
