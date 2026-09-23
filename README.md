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

專案根目錄也提供了啟動腳本，會自動切到 `MyAjaxApi` 執行 `dotnet watch run --no-hot-reload`，不必先 `cd`：

| 腳本 | 執行方式 | 說明 |
| ---- | -------- | ---- |
| `start.bat` | `.\start.bat`（cmd 或 PowerShell 皆可） | 不受 PowerShell 執行原則限制，最省事 |
| `start.ps1` | `.\start.ps1` | 需先放行執行原則，見下方 |
| macOS / Linux | `cd MyAjaxApi && dotnet watch run` | 直接下指令即可 |

Windows PowerShell 預設執行原則是 `Restricted`，直接執行 `.\start.ps1` 會出現「因為這個系統上已停用指令碼執行」的錯誤。擇一處理：

```powershell
# 只放寬這一次，不改系統設定
powershell -ExecutionPolicy Bypass -File .\start.ps1

# 或：設定一次，之後直接 .\start.ps1（只影響目前使用者，不需要系統管理員）
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

> 若專案是從網路下載的 zip 解壓出來，Windows 會把檔案標記為「來自網際網路」，RemoteSigned 仍會擋，先執行 `Unblock-File .\start.ps1` 解除封鎖。

啟動後開啟瀏覽器：

| 頁面 | 網址 | 對應講義 |
| ---- | ---- | -------- |
| 待辦清單（Todo App） | <http://localhost:5269/> | 6-6 |
| 水果清單：搜尋、排序、分頁、編輯、欄位驗證 | <http://localhost:5269/fruits.html> | 5-5 至 5-10、6-3、8-4 |
| 商品管理（EF Core 版） | <http://localhost:5269/products.html> | 5-7、7-4 |
| 檔案上傳：fetch + FormData 與 XHR 進度條 | <http://localhost:5269/upload.html> | 6-1 |
| 同步 vs 非同步 XHR | <http://localhost:5269/sync-demo.html> | 4-3 |
| SSE 伺服器推送 | <http://localhost:5269/sse-demo.html> | 6-2 |
| JWT 登入、註冊、查看 token | <http://localhost:5269/login.html> | 第 9 章 |
| Swagger API 文件 | <http://localhost:5269/swagger> | 2-5 |

> 第一次啟動會自動建立 `app.db`（SQLite），不需要手動執行 Migration。
> `EnsureCreated()` 只在資料庫不存在時建表，若 `app.db` 是加入 `Users` 之前建立的，請先刪掉再啟動。

> 啟動前必須先設定 JWT 簽章金鑰（見下方「JWT 登入」一節），否則會在啟動時報錯。

> 待辦清單（`/api/todos`）需要登入才能操作，其餘端點不需登入。`api.js` 會自動把 localStorage 的 token 放進 `Authorization` 標頭，遇 401 則清除 token。

> `MyAjaxApi.http` 收錄了所有端點的測試請求，用 VS Code 的 REST Client 擴充套件或 Visual Studio 2022 開啟即可逐一送出（講義 1-6）。

---

## JWT 登入

### 設定簽章金鑰（只需一次）

金鑰不放在 `appsettings.json`（會進 git），改用 .NET 的 user-secrets 存在使用者目錄下：

macOS／Linux：

```bash
cd MyAjaxApi
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

Windows（PowerShell）：

```powershell
cd MyAjaxApi
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]]))
```

Windows（cmd）或任何環境，直接打一串至少 32 個字元的亂碼也可以：

```cmd
cd MyAjaxApi
dotnet user-secrets set "Jwt:Key" "any-random-string-at-least-32-characters-long-here"
```

指令要在有 `.csproj` 的 `MyAjaxApi` 資料夾執行。設定完用 `dotnet user-secrets list` 確認有 `Jwt:Key`。`Issuer`、`Audience`、`ExpireMinutes` 在 `appsettings.json` 的 `Jwt` 區段。

金鑰實際存在使用者目錄，不在專案裡：macOS／Linux 是 `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`，Windows 是 `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`。`<UserSecretsId>` 就是 `MyAjaxApi.csproj` 裡的那串 GUID，已進 git。因此：

- 同一台電腦重新 clone：不用重設，csproj 帶著同一個 ID，啟動時會自動找到。
- 換一台電腦：只要再執行一次上面的 `set` 指令，不需要 `dotnet user-secrets init`。
- user-secrets 只在 Development 環境生效。`dotnet run`、`dotnet watch run`、Visual Studio 啟動都會套用 launchSettings.json 的 Development；直接執行 bin 底下的 exe 則是 Production，會讀不到。正式環境改用環境變數 `Jwt__Key`（雙底線代表冒號）。

### 流程

1. `POST /api/auth/register`：密碼用 bcrypt 雜湊後存入 `Users` 資料表，明文不落地。
2. `POST /api/auth/login`：比對密碼，成功後由 `TokenService` 簽發 JWT，回傳 `{ token, expiresAt, username }`。
3. 前端把 token 存進 localStorage，`api.js` 之後的每個請求都帶 `Authorization: Bearer <token>`。
4. 加了 `[Authorize]` 的端點（`/api/auth/me`、`/api/todos`）由 JwtBearer 中介軟體驗證簽章、簽發者、接收者與到期時間，不通過直接回 401。
5. 登出只是前端丟掉 token；JWT 是無狀態的，已簽發的 token 在到期前仍然有效。

### 測試方式

**瀏覽器**

1. 開 <http://localhost:5269/>，未登入時會看到提示、表單被隱藏。
2. 到 <http://localhost:5269/login.html> 註冊（例如 `alice` / `secret123`），再登入。頁面會顯示目前帳號、token 原文與解碼後的 Payload。
3. 回待辦清單頁，可以新增、勾選、刪除。
4. 錯誤情境：帳號填 `a` 會在欄位旁顯示驗證訊息；密碼打錯回「帳號或密碼錯誤」；重複註冊回「帳號已被使用」。
5. 按「登出」後回待辦頁，會變回未登入提示。
6. 想看 token 過期，把 `appsettings.json` 的 `ExpireMinutes` 改成 1，登入後等一分鐘再操作待辦頁。

**REST Client（`MyAjaxApi.http`）**

「JWT 登入：Auth」區段依序送出：註冊、登入、帶 token 查 `/me`、不帶 token、竄改 token。登入請求上方有 `# @name login`，後續請求用 `{{login.response.body.token}}` 自動帶入 token，Todos 區段的請求也一樣。這個方式能看到完整的 `Authorization` 標頭與回應 Body，最適合講解。

**Swagger UI**

1. 執行 `POST /api/auth/login` 取得 token。
2. 點右上角 Authorize，貼上 token（不用加 `Bearer ` 前綴）。
3. 之後執行 `/api/auth/me` 或 `/api/todos` 都會自動帶 token；按 Logout 再試會變 401。

**確認密碼有雜湊**

```bash
sqlite3 MyAjaxApi/app.db "select Username, PasswordHash from Users;"
```

應看到 `$2a$11$` 開頭的 60 字元字串，而非明文。

### 常見問題

- 啟動時報「Jwt:Key 未設定或太短」：user-secrets 沒設，執行上方的 `dotnet user-secrets set` 指令。
- 待辦頁一直 500 或找不到 `Users` 資料表：`app.db` 是加入 Users 之前建立的，刪掉再重啟。
- `dotnet watch` 改了 `Program.cs` 或新增 Controller 後仍回舊結果：這類變更無法熱重載，重啟 watch。

### 刻意不做的事

- 沒有 refresh token、沒有登出黑名單、沒有角色權限，重點放在「簽發、攜帶、驗證」三步驟。
- 沒有用 ASP.NET Core Identity，自己寫 `Users` 資料表才看得清楚每一步在做什麼。

---

## 專案結構

```
MyAjaxApi/
├── Controllers/
│   ├── BasicsController.cs         # GET /api/basics，最簡單的 Controller（講義 3-1）
│   ├── FruitsController.cs         # In-Memory CRUD /api/fruits，含搜尋/排序/分頁、slow、upload（3-4、5-8、4-3、6-1）
│   ├── TodosController.cs          # EF Core CRUD /api/todos（6-6、7-4）
│   ├── ProductsController.cs       # EF Core CRUD /api/products（7-4）
│   ├── NotificationsController.cs  # SSE /api/notifications/stream（6-2）
│   └── AuthController.cs           # JWT 註冊、登入、/me（9-6）
├── Data/
│   └── AppDbContext.cs             # EF Core DbContext，含 CreatedAt 的 UTC 值轉換器（5-7）
├── Infrastructure/
│   └── GlobalExceptionHandler.cs   # 全域例外處理（8-3）
├── Models/
│   ├── Fruit.cs                    # Fruit + DTOs（3-3）
│   ├── PagedResult.cs              # 分頁回應外殼（5-8）
│   ├── Product.cs                  # Product + DTOs（7-2）
│   ├── Todo.cs                     # Todo + DTOs（6-6）
│   └── User.cs                     # User + 註冊、登入 DTOs（9-3）
├── Services/
│   └── TokenService.cs             # 簽發 JWT；JwtSettings 對應 appsettings 的 Jwt 區段（9-4）
├── Validators/                     # FluentValidation 驗證規則（8-2）
│   ├── CreateFruitValidator.cs
│   ├── UpdateFruitValidator.cs
│   ├── CreateProductValidator.cs
│   ├── UpdateProductValidator.cs
│   ├── RegisterValidator.cs
│   └── LoginValidator.cs
├── wwwroot/
│   ├── index.html                  # 待辦清單頁（需登入，9-7）
│   ├── login.html                  # 登入、註冊、查看 token（9-7）
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
└── appsettings.json                # 連線字串、Jwt 設定（Key 在 user-secrets）
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

# JWT 驗證與密碼雜湊
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next

# 以下兩個是為了修補傳遞相依套件的已知弱點而明確指定版本，見 .csproj 的註解
dotnet add package SQLitePCLRaw.bundle_e_sqlite3
dotnet add package Microsoft.OpenApi
```

### 2-1. 設定 JWT 金鑰

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

`dotnet user-secrets init` 只做一件事：產生一個 GUID 寫進 `.csproj` 的 `<UserSecretsId>`。這個 ID 是專案與機密儲存位置的對應鍵，之後 `set` 會把值寫到使用者目錄下以該 ID 命名的資料夾（路徑見上方「設定簽章金鑰」），程式啟動時設定系統也靠同一個 ID 找到檔案。

- 只需執行一次：ID 隨 csproj 進 git，其他電腦 clone 後直接 `set` 即可。重複執行 `init` 會發現已有 ID 而跳過，不會換新。
- 沒 `init` 就 `set` 會報找不到 `UserSecretsId`，因為工具不知道該存去哪裡。
- Development 環境的設定來源由低到高：`appsettings.json`、`appsettings.Development.json`、user-secrets、環境變數、命令列參數，後者覆蓋前者。

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

測試端點前要先把伺服器跑起來（見上方「啟動方式」，`.\start.bat` 或到 `MyAjaxApi` 執行 `dotnet watch run`），所有路徑的主機都是 <http://localhost:5269>。要逐一送出請求，用 VS Code REST Client 開 `MyAjaxApi.http`，或開 <http://localhost:5269/swagger>。表格最後一欄是講義的對應章節。

### Basics

| 方法 | 路徑 | 說明 | 講義 |
|------|------|------|------|
| GET | `/api/basics` | 回傳 `{ "message": "Hello API" }` | 3-1 |

### Fruits（In-Memory，重啟後歸零）

| 方法 | 路徑 | 說明 | 講義 |
|------|------|------|------|
| GET | `/api/fruits?name=&sort=&page=&size=` | 列表；`sort` 可為 `id`、`name`、`price`、`price_desc`；回傳 `{ items, total, page, size, totalPages }` | 5-8、5-9 |
| GET | `/api/fruits/{id}` | 取得單筆 | 3-4 |
| POST | `/api/fruits` | 新增（名稱 1 到 20 字、價格 1 到 10000） | 3-4、8-2、8-4 |
| PUT | `/api/fruits/{id}` | 完整更新 | 3-4、5-10 |
| DELETE | `/api/fruits/{id}` | 刪除 | 3-4、6-3 |
| GET | `/api/fruits/slow?seconds=10` | 等待指定秒數（1 到 30）後回傳全部，供同步 vs 非同步示範 | 4-3 |
| POST | `/api/fruits/upload` | `multipart/form-data` 上傳圖片（jpg、png、gif、webp，最大 2 MB），存到 `wwwroot/uploads` | 6-1 |

### Auth（JWT）

| 方法 | 路徑 | 說明 | 講義 |
|------|------|------|------|
| POST | `/api/auth/register` | 註冊（帳號 3 到 20 字英數底線、密碼至少 6 字）；重複回 409 | 9-3、9-6 |
| POST | `/api/auth/login` | 登入，回傳 `{ token, expiresAt, username }`；失敗回 401 | 9-4、9-6 |
| GET | `/api/auth/me` | 需帶 token，回傳 `{ id, username }` | 9-5 |

### Todos（EF Core，需帶 token）

整個 Controller 加了 `[Authorize]`（講義 9-5），沒帶或帶無效 token 一律 401。

| 方法 | 路徑 | 說明 | 講義 |
|------|------|------|------|
| GET | `/api/todos` | 取得所有待辦 | 6-6、7-4 |
| GET | `/api/todos/{id}` | 取得單筆 | 6-6 |
| POST | `/api/todos` | 新增待辦 | 6-6、7-4 |
| PATCH | `/api/todos/{id}` | 更新完成狀態 | 6-6 |
| DELETE | `/api/todos/{id}` | 刪除待辦 | 6-6、6-3 |

### Products（EF Core）

| 方法 | 路徑 | 說明 | 講義 |
|------|------|------|------|
| GET | `/api/products` | 取得所有商品（最新的在前） | 7-4 |
| GET | `/api/products/{id}` | 取得單筆商品 | 7-4 |
| POST | `/api/products` | 新增商品（`imageUrl` 選填，有填必須是 https） | 7-4、8-2 |
| PUT | `/api/products/{id}` | 更新商品 | 7-4 |
| DELETE | `/api/products/{id}` | 刪除商品 | 7-4 |

### Notifications（SSE）

| 方法 | 路徑 | 說明 | 講義 |
|------|------|------|------|
| GET | `/api/notifications/stream` | `text/event-stream`，每 2 秒推送 `{ seq, time, fruitCount }` | 6-2 |
