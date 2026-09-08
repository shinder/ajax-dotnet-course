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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite 沒有時區概念，DateTime 存進去再讀出來 Kind 會變成 Unspecified，
        // 序列化成 JSON 就少了結尾的 Z，前端 new Date() 會把它當本地時間，時區不同就會差好幾小時。
        // 用值轉換器在讀出時標記為 UTC（寫入的值本來就是 DateTime.UtcNow），JSON 才會正確帶 Z。
        modelBuilder.Entity<Product>()
            .Property(p => p.CreatedAt)
            .HasConversion(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
