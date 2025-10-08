# 設計文件

## 專案概述

MOTP 雙重驗證網站是一個基於 TOTP 標準的多因素身份驗證平台，提供安全的兩階段登入流程。系統採用前後端分離架構，前端使用 Angular 框架，後端使用 ASP.NET Core Web API，支援 Microsoft Authenticator 和 Google Authenticator 等主流認證應用程式。

## 技術標準對齊

### 前端技術棧
- Angular 17+: 最新版本的 Angular 框架
- TypeScript 5.0+: 強型別 JavaScript 超集
- Angular Material: Google 設計系統元件庫
- RxJS: 響應式程式設計
- QR Code 生成庫: qrcode.js

### 後端技術棧
- C# .NET 8: 最新版本的 .NET 平台
- ASP.NET Core Web API: RESTful API 框架
- Dapper: ORM 資料存取框架
- SQL Server: 關聯式資料庫
- Swagger/OpenAPI: API 文件生成
- TOTP 實作: 使用 RFC 6238 標準

## 系統架構

### 三層式架構
- **Controller 層**: API 端點和請求處理
- **Service 層**: 商業邏輯和 TOTP 處理
- **Repository 層**: 資料存取和資料庫操作
- **Common 層**: 共用工具和元件

### 模組化設計原則
- **單一職責原則**: 每個檔案處理單一特定功能
- **元件隔離**: 建立小型、專注的元件
- **服務層分離**: 分離資料存取、商業邏輯和展示層
- **工具模組化**: 將工具分解為專注的單一用途模組

## 資料模型

### User 實體
- id: Guid (主鍵)
- email: string (唯一索引)
- passwordHash: string (bcrypt 雜湊)
- isEmailVerified: bool
- createdAt: DateTime
- updatedAt: DateTime
- failedLoginAttempts: int
- isLocked: bool
- lockedUntil: DateTime?

### OTPCode 實體
- id: Guid (主鍵)
- userId: Guid (外鍵)
- secretKey: string (AES-256 加密)
- deviceName: string
- isActive: bool
- createdAt: DateTime
- lastUsedAt: DateTime?

### BackupCode 實體
- id: Guid (主鍵)
- userId: Guid (外鍵)
- code: string (雜湊)
- isUsed: bool
- createdAt: DateTime
- usedAt: DateTime?

## API 設計

### 認證 API
- POST /api/auth/register: 用戶註冊
- POST /api/auth/login: 第一階段登入（帳號/密碼）
- POST /api/auth/verify-otp: 第二階段登入（MOTP 驗證）
- POST /api/auth/logout: 用戶登出
- POST /api/auth/refresh-token: 刷新 Token
- POST /api/auth/verify-backup-code: 備用代碼驗證

### OTP API
- POST /api/otp/generate-qr: 生成 QR Code
- POST /api/otp/verify-setup: 驗證裝置綁定
- POST /api/otp/verify-code: 驗證 TOTP 代碼
- GET /api/otp/devices: 取得用戶裝置列表
- DELETE /api/otp/devices/{id}: 移除裝置
- POST /api/otp/generate-backup-codes: 生成備用代碼

### 用戶管理 API
- GET /api/user/profile: 取得用戶資料
- PUT /api/user/profile: 更新用戶資料
- GET /api/user/login-history: 取得登入記錄

## 安全設計

### JWT Token 認證
- Access Token: 15 分鐘過期
- Refresh Token: 7 天過期
- 角色基礎授權 (Role-based Authorization)

### 加密與安全
- API Key 驗證
- bcrypt 密碼雜湊
- HTTPS 通訊加密
- 資料庫加密
- CORS 跨域請求控制
- 速率限制
- OTP 過期機制（30 秒）
- CSRF 保護
- XSS 防護
- 帳戶鎖定機制

## 前端架構設計

### Angular 模組結構
- **AuthModule**: 認證相關元件和服務
- **OTPModule**: OTP 相關元件和服務
- **DashboardModule**: 用戶儀表板
- **SharedModule**: 共用元件和服務
- **CoreModule**: 核心服務和攔截器

### 元件設計
- **LoginComponent**: 第一階段登入（帳號/密碼）
- **OTPVerificationComponent**: 第二階段登入（MOTP）
- **RegisterComponent**: 用戶註冊
- **QRCodeSetupComponent**: QR Code 設定
- **DeviceManagementComponent**: 裝置管理
- **BackupCodeComponent**: 備用代碼管理

### 服務設計
- **AuthService**: 認證服務
- **OTPService**: OTP 相關服務
- **QRCodeService**: QR Code 生成服務
- **HttpInterceptorService**: HTTP 攔截器

## 效能優化

### 前端優化
- 懶加載 (Lazy Loading)
- OnPush 變更檢測策略
- Tree Shaking
- Bundle 優化
- 圖片壓縮和延遲載入

### 後端優化
- 快取機制 (Redis)
- 資料庫索引
- 連線池
- 非同步處理 (Async/Await)
- 分頁查詢

## 測試策略

### 單元測試
- 後端: xUnit 測試框架
- 前端: Jasmine 和 Karma
- 模擬依賴項 (Moq)

### 整合測試
- API 端點測試 (Postman/Newman)
- 前後端整合測試
- 端對端測試 (Cypress)
- 效能測試 (JMeter)

## 部署架構

### 容器化
- Docker 容器
- Docker Compose
- Kubernetes 編排

### CI/CD
- GitHub Actions
- 自動化測試
- 自動化部署
- 環境管理
