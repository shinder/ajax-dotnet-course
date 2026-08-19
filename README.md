# AJAX + RESTful API 課程範例專案

ASP.NET Core 10 Web API + Vanilla JS 前端，作為課程講義的對應範例。

## 系統需求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [dotnet-ef 工具](https://learn.microsoft.com/ef/core/cli/dotnet)（建立 Migration 時才需要）

---

## 啟動方式

```bash
cd MyAjaxApi
dotnet run
```

啟動後開啟瀏覽器：

- 前端首頁（待辦清單）：<http://localhost:5269>
- Swagger API 文件：<http://localhost:5269/swagger>

> 第一次啟動會自動建立 `app.db`（SQLite），不需要手動執行 Migration。

> 本範例不需要登入即可操作；`api.js` 仍保留「有 token 就帶上 `Authorization`、遇 401 清除 token」的通用寫法作為示範。完整登入／JWT 見另一門課程。

---

## 專案結構

```
MyAjaxApi/
├── Controllers/
│   ├── ProductsController.cs   # CRUD /api/products
│   └── TodosController.cs      # CRUD /api/todos
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext
├── Infrastructure/
│   └── GlobalExceptionHandler.cs
├── Models/
│   ├── Product.cs              # Product + DTOs
│   └── Todo.cs                 # Todo + DTOs
├── Validators/
│   ├── CreateProductValidator.cs   # FluentValidation 驗證規則
│   └── UpdateProductValidator.cs
├── wwwroot/
│   ├── index.html              # 待辦清單頁
│   ├── products.html           # 商品管理頁
│   ├── js/
│   │   ├── api.js              # Fetch 封裝（apiFetch wrapper）
│   │   └── toast.js            # Toast 通知元件
│   └── css/
│       └── style.css
├── Program.cs                  # 服務注入 + Middleware 設定
└── appsettings.json            # 連線字串設定
```

---

## 從零建立專案的指令流程

### 1. 建立專案

```bash
dotnet new webapi -n MyAjaxApi --use-controllers
cd MyAjaxApi
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package FluentValidation.AspNetCore
dotnet add package Swashbuckle.AspNetCore
```

### 3. 安裝 EF Core CLI 工具（全域，只需一次）

```bash
dotnet tool install --global dotnet-ef
```

### 4. 建立 Migration（手動管理資料庫版本時使用）

> 本範例改用 `db.Database.EnsureCreated()` 自動建表，  
> 正式專案建議改用 Migration 管理結構變更。

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. 執行

```bash
dotnet run
```

---

## API 端點

### Todos

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/todos` | 取得所有待辦 |
| POST | `/api/todos` | 新增待辦 |
| PATCH | `/api/todos/{id}` | 更新完成狀態 |
| DELETE | `/api/todos/{id}` | 刪除待辦 |

### Products

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/products` | 取得所有商品 |
| GET | `/api/products/{id}` | 取得單筆商品 |
| POST | `/api/products` | 新增商品 |
| PUT | `/api/products/{id}` | 更新商品 |
| DELETE | `/api/products/{id}` | 刪除商品 |
