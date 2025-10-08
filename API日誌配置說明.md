# API 日誌配置說明

## 📍 當前日誌狀態

### 🔧 目前配置
**日誌提供者**：預設的 ASP.NET Core 日誌系統
- **控制台日誌**：✅ 已啟用（開發環境預設）
- **檔案日誌**：❌ 未配置
- **結構化日誌**：❌ 未配置

### 📂 日誌存放位置

#### 當前狀況：
- **控制台輸出**：直接顯示在 `dotnet run` 的命令列視窗中
- **檔案日誌**：無（需要額外配置）
- **系統事件日誌**：無

---

## 🔧 如何查看當前日誌

### 1. 開發環境（dotnet run）
```bash
cd MOTPDualAuthWebsite.API
dotnet run
```
日誌會直接顯示在控制台中，包括：
- 應用程式啟動資訊
- HTTP 請求日誌
- 資料庫操作日誌
- 自定義業務日誌（登入、驗證等）

### 2. 日誌格式範例
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7062
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5062
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: MOTPDualAuthWebsite.API.Controllers.AuthController[0]
      收到登入請求，帳號: test@example.com
info: MOTPDualAuthWebsite.API.Services.AuthService[0]
      開始認證使用者 test@example.com 從 IP 127.0.0.1
```

---

## 📝 建議的日誌配置改進

### 1. 添加檔案日誌提供者

#### 安裝 NuGet 套件
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
```

#### 更新 Program.cs
```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/motp-api-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            rollOnFileSizeLimit: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        );
});
```

#### 更新 appsettings.json
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/motp-api-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### 2. 建議的日誌目錄結構
```
MOTPDualAuthWebsite.API/
├── logs/
│   ├── motp-api-20240925.txt
│   ├── motp-api-20240924.txt
│   └── motp-api-20240923.txt
├── Program.cs
└── appsettings.json
```

---

## 📊 日誌分類和等級

### 當前使用的日誌等級

#### Information 等級
- 使用者登入/登出
- TOTP 驗證成功/失敗
- API 請求處理
- 業務流程追蹤

```csharp
_logger.LogInformation("使用者 {Email} 登入成功", email);
_logger.LogInformation("開始認證使用者 {Email} 從 IP {IpAddress}", email, ipAddress);
```

#### Warning 等級
- 帳號鎖定
- 驗證失敗
- 異常但可處理的情況

```csharp
_logger.LogWarning("使用者 {Email} 密碼驗證失敗", email);
_logger.LogWarning("帳號 {Email} 目前被鎖定至 {LockedUntil}", email, lockedUntil);
```

#### Error 等級
- 系統錯誤
- 資料庫連線問題
- 未預期的例外

```csharp
_logger.LogError(ex, "認證使用者 {Email} 時發生錯誤", email);
_logger.LogError(ex, "產生 JWT Token 時發生錯誤");
```

---

## 🔍 日誌監控和分析

### 1. 重要日誌事件

#### 安全相關
- 登入失敗嘗試
- 帳號鎖定事件
- TOTP 驗證失敗
- JWT Token 驗證失敗

#### 系統效能
- API 回應時間
- 資料庫查詢時間
- 記憶體使用情況

#### 業務指標
- 使用者註冊數量
- 登入成功率
- TOTP 使用統計

### 2. 日誌查詢範例

#### 查看今日的登入失敗
```bash
# 在 logs 目錄中
grep "密碼驗證失敗" motp-api-20240925.txt
```

#### 查看特定使用者的活動
```bash
grep "test@example.com" motp-api-20240925.txt
```

#### 查看錯誤日誌
```bash
grep "\[ERR\]" motp-api-20240925.txt
```

---

## 🚀 快速設定檔案日誌

如果您想立即啟用檔案日誌，可以執行以下步驟：

### 1. 安裝 Serilog
```bash
cd MOTPDualAuthWebsite.API
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

### 2. 建立 logs 目錄
```bash
mkdir logs
```

### 3. 簡單配置
在 `Program.cs` 的開頭添加：
```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 簡單的檔案日誌配置
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/motp-api.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

---

## 📋 生產環境建議

### 1. 日誌輪轉
- 每日輪轉檔案
- 保留 30 天的日誌
- 單檔案大小限制 10MB

### 2. 敏感資訊過濾
- 不記錄密碼
- 不記錄完整的 JWT Token
- 不記錄信用卡資訊

### 3. 結構化日誌
- 使用 JSON 格式
- 包含關聯 ID (Correlation ID)
- 標準化錯誤代碼

### 4. 監控整合
- ELK Stack (Elasticsearch, Logstash, Kibana)
- Application Insights
- Grafana + Loki

---

## ⚠️ 注意事項

1. **磁碟空間**：定期清理舊日誌檔案
2. **效能影響**：過多的日誌會影響效能
3. **安全性**：日誌檔案包含敏感資訊，需要適當的存取控制
4. **備份**：重要日誌需要備份策略

---

## 🔧 故障排除

### 日誌檔案無法建立
- 檢查目錄權限
- 確認磁碟空間
- 驗證路徑正確性

### 日誌等級設定無效
- 檢查 appsettings.json 格式
- 確認環境變數設定
- 重新啟動應用程式

### 效能問題
- 降低日誌等級
- 減少日誌輸出頻率
- 使用異步日誌寫入

目前系統使用預設的控制台日誌，建議升級到檔案日誌以便長期監控和分析！ 