using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAjaxApi.Data;
using MyAjaxApi.Models;

namespace MyAjaxApi.Controllers;

// [ApiController]：啟用 Web API 專屬行為：
//   - 自動驗證 Model，失敗時直接回傳 400，不需要手動判斷 ModelState.IsValid
//   - 複雜型別參數（class）預設從 Body 讀取，不需要每個都加 [FromBody]
//   - 回傳標準 Problem Details 格式的錯誤
//
// [Route("api/[controller]")]：[controller] 會替換為類別名稱去掉 "Controller" 後綴，
//   ProductsController → "products"，所以路由是 /api/products
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // 透過建構式注入 DbContext（Dependency Injection）。
    // ASP.NET Core 的 DI 容器會自動把 AppDbContext 的實例傳進來，
    // 不需要自己 new AppDbContext()。
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    // GET /api/products
    // 取得所有商品，依建立時間倒序排列（最新的在前）
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        // ToListAsync()：非同步執行 SQL，等待期間執行緒可處理其他請求
        // 等同於：SELECT * FROM Products ORDER BY CreatedAt DESC
        var products = await _db.Products
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Ok() 回傳 HTTP 200 + JSON 序列化的 products
        return Ok(products);
    }

    // GET /api/products/5
    // {id:int}：路由約束，只接受整數，傳非整數時直接 404，不進 Action
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        // FindAsync：用主鍵查詢，比 FirstOrDefaultAsync 效能更好，
        // 因為會先查 DbContext 的本地快取，找不到才去資料庫
        var product = await _db.Products.FindAsync(id);

        // is null：C# 8 的 pattern matching，等同 product == null
        if (product is null) return NotFound();   // 404

        return Ok(product);   // 200
    }

    // POST /api/products
    // 新增商品，從 Request Body 讀取 JSON（因為有 [ApiController]，不需要標 [FromBody]）
    [HttpPost]
    public async Task<ActionResult<Product>> Create(CreateProductDto dto)
    {
        // 把 DTO 轉換成 Domain Model，補上伺服器決定的欄位
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            CreatedAt = DateTime.UtcNow   // 時間由伺服器設定，不信任用戶端
        };

        _db.Products.Add(product);        // 追蹤這筆資料（尚未寫入 DB）
        await _db.SaveChangesAsync();     // 執行 INSERT SQL

        // CreatedAtAction 回傳 HTTP 201 Created，
        // 並在 Response Header 加上 Location: /api/products/5
        // 告訴用戶端新資源在哪裡
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /api/products/5
    // 完整更新（所有欄位都要送，即使沒改）
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        // 修改 EF Core 正在追蹤的物件屬性，
        // SaveChangesAsync 時會自動產生 UPDATE SQL
        product.Name = dto.Name;
        product.Price = dto.Price;
        await _db.SaveChangesAsync();

        // 204 No Content：更新成功，不回傳資料（前端不需要）
        return NoContent();
    }

    // DELETE /api/products/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        _db.Products.Remove(product);   // 標記為刪除
        await _db.SaveChangesAsync();   // 執行 DELETE SQL

        return NoContent();   // 204
    }
}
