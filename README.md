# AJAX + RESTful API 課程範例專案

ASP.NET Core 10 Web API + 原生 JavaScript 前端，作為課程講義《AJAX + RESTful API 使用 .NET》的對應範例。講義每一節標示「範例：」的地方，都可以在這裡找到對應的檔案。

## 系統需求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [dotnet-ef 工具](https://learn.microsoft.com/ef/core/cli/dotnet)（建立 Migration 時才需要）

---

## 啟動方式

```bash
cd MyAjaxApi
dotnet watch run     # 或 dotnet run；watch 會在改檔後自動重新編譯（講義 2-2）
```

啟動後開啟瀏覽器：

| 頁面 | 網址 | 對應講義 |
| ---- | ---- | -------- |
| 待辦清單（Todo App） | <http://localhost:5269/> | 6-6 |
| 水果清單：搜尋、排序、分頁、編輯、欄位驗證 | <http://localhost:5269/fruits.html> | 5-5 至 5-10、6-3、8-4 |
| 商品管理（EF Core 版） | <http://localhost:5269/products.html> | 5-7、7-4 |
| 檔案上傳：fetch + FormData 與 XHR 進度條 | <http://localhost:5269/upload.html> | 6-1 |
| 同步 vs 非同步 XHR | <http://localhost:5269/sync-demo.html> | 4-3 |
| SSE 伺服器推送 | <http://localhost:5269/sse-demo.html> | 6-2 |
| Swagger API 文件 | <http://localhost:5269/swagger> | 2-5 |

> 第一次啟動會自動建立 `app.db`（SQLite），不需要手動執行 Migration。

> 本範例不需要登入即可操作；`api.js` 仍保留「有 token 就帶上 `Authorization`、遇 401 清除 token」的通用寫法作為示範。完整登入／JWT 見另一門課程。

> `MyAjaxApi.http` 收錄了所有端點的測試請求，用 VS Code 的 REST Client 擴充套件或 Visual Studio 2022 開啟即可逐一送出（講義 1-6）。

---

## 專案結構

```
MyAjaxApi/
├── Controllers/
│   ├── BasicsController.cs         # GET /api/basics，最簡單的 Controller（講義 3-1）
│   ├── FruitsController.cs         # In-Memory CRUD /api/fruits，含搜尋/排序/分頁、slow、upload（3-4、5-8、4-3、6-1）
│   ├── TodosController.cs          # EF Core CRUD /api/todos（6-6、7-4）
│   ├── ProductsController.cs       # EF Core CRUD /api/products（7-4）
│   └── NotificationsController.cs  # SSE /api/notifications/stream（6-2）
├── Data/
│   └── AppDbContext.cs             # EF Core DbContext，含 CreatedAt 的 UTC 值轉換器（5-7）
├── Infrastructure/
│   └── GlobalExceptionHandler.cs   # 全域例外處理（8-3）
├── Models/
│   ├── Fruit.cs                    # Fruit + DTOs（3-3）
│   ├── PagedResult.cs              # 分頁回應外殼（5-8）
│   ├── Product.cs                  # Product + DTOs（7-2）
│   └── Todo.cs                     # Todo + DTOs（6-6）
├── Validators/                     # FluentValidation 驗證規則（8-2）
│   ├── CreateFruitValidator.cs
│   ├── UpdateFruitValidator.cs
│   ├── CreateProductValidator.cs
│   └── UpdateProductValidator.cs
├── wwwroot/
│   ├── index.html                  # 待辦清單頁
│   ├── fruits.html                 # 水果清單頁（第 5 章前端範例的集合）
│   ├── products.html               # 商品管理頁
│   ├── upload.html                 # 檔案上傳頁
│   ├── sync-demo.html              # 同步 vs 非同步示範
│   ├── sse-demo.html               # SSE 推送示範
│   ├── uploads/                    # 上傳的檔案（已 gitignore，dotnet watch 也不監看）
│   ├── js/
│   │   ├── api.js                  # fetch 封裝（5-4）
│   │   ├── toast.js                # Toast 通知（5-6）
│   │   └── utils.js                # escapeHtml、debounce（5-5、5-9）
│   └── css/
│       └── style.css
├── MyAjaxApi.http                  # 所有端點的測試請求
├── Program.cs                      # 服務注入 + Middleware 設定（2-4）
└── appsettings.json                # 連線字串設定
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

# 以下兩個是為了修補傳遞相依套件的已知弱點而明確指定版本，見 .csproj 的註解
dotnet add package SQLitePCLRaw.bundle_e_sqlite3
dotnet add package Microsoft.OpenApi
```

### 3. 安裝 EF Core CLI 工具（全域，只需一次）

```bash
dotnet tool install --global dotnet-ef
```

### 4. 建立 Migration（手動管理資料庫版本時使用）

> 本範例改用 `db.Database.EnsureCreated()` 自動建表，
> 正式專案建議改用 Migration 管理結構變更（講義 7-3）。

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. 執行

```bash
dotnet watch run
```

---

## API 端點

### Basics

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/basics` | 回傳 `{ "message": "Hello API" }` |

### Fruits（In-Memory，重啟後歸零）

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/fruits?name=&sort=&page=&size=` | 列表；`sort` 可為 `id`、`name`、`price`、`price_desc`；回傳 `{ items, total, page, size, totalPages }` |
| GET | `/api/fruits/{id}` | 取得單筆 |
| POST | `/api/fruits` | 新增（名稱 1 到 20 字、價格 1 到 10000） |
| PUT | `/api/fruits/{id}` | 完整更新 |
| DELETE | `/api/fruits/{id}` | 刪除 |
| GET | `/api/fruits/slow?seconds=10` | 等待指定秒數（1 到 30）後回傳全部，供同步 vs 非同步示範 |
| POST | `/api/fruits/upload` | `multipart/form-data` 上傳圖片（jpg、png、gif、webp，最大 2 MB），存到 `wwwroot/uploads` |

### Todos（EF Core）

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/todos` | 取得所有待辦 |
| GET | `/api/todos/{id}` | 取得單筆 |
| POST | `/api/todos` | 新增待辦 |
| PATCH | `/api/todos/{id}` | 更新完成狀態 |
| DELETE | `/api/todos/{id}` | 刪除待辦 |

### Products（EF Core）

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/products` | 取得所有商品（最新的在前） |
| GET | `/api/products/{id}` | 取得單筆商品 |
| POST | `/api/products` | 新增商品（`imageUrl` 選填，有填必須是 https） |
| PUT | `/api/products/{id}` | 更新商品 |
| DELETE | `/api/products/{id}` | 刪除商品 |

### Notifications（SSE）

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/notifications/stream` | `text/event-stream`，每 2 秒推送 `{ seq, time, fruitCount }` |
