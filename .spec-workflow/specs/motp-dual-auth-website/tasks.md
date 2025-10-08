# 任務文件

## 後端開發任務

- [x] 1. 建立資料庫模型
  - 檔案: Models/User.cs, Models/OTPCode.cs, Models/BackupCode.cs
  - 定義用戶、OTP 和備用代碼資料模型
  - 設定 Entity Framework 關聯和索引
  - 目的: 建立資料層基礎

- [-] 2. 建立 Repository 層
  - 檔案: Repositories/IUserRepository.cs, Repositories/UserRepository.cs
  - 檔案: Repositories/IOTPRepository.cs, Repositories/OTPRepository.cs
  - 檔案: Repositories/IBackupCodeRepository.cs, Repositories/BackupCodeRepository.cs
  - 實作資料存取介面和實作
  - 包含 CRUD 操作和查詢方法
  - 目的: 提供資料存取抽象層

- [ ] 3. 建立 Service 層
  - 檔案: Services/IAuthService.cs, Services/AuthService.cs
  - 檔案: Services/IOTPService.cs, Services/OTPService.cs
  - 檔案: Services/IEncryptionService.cs, Services/EncryptionService.cs
  - 實作認證和 OTP 業務邏輯
  - 包含密碼雜湊、JWT Token 生成、TOTP 處理
  - 目的: 提供業務邏輯層

- [ ] 4. 建立 Controller 層
  - 檔案: Controllers/AuthController.cs, Controllers/OTPController.cs
  - 檔案: Controllers/UserController.cs
  - 實作 API 端點
  - 包含 Swagger 文件註解
  - 目的: 提供 API 介面層

- [ ] 5. 設定依賴注入和配置
  - 檔案: Program.cs, appsettings.json
  - 註冊所有服務和 Repository
  - 設定 Entity Framework DbContext
  - 配置 JWT 和加密設定
  - 目的: 配置依賴注入容器

## 前端開發任務

- [ ] 6. 建立 Angular 專案結構
  - 設定 Angular CLI 專案
  - 安裝 Angular Material
  - 設定路由和模組結構
  - 配置 TypeScript 和 ESLint
  - 目的: 建立前端專案基礎

- [ ] 7. 建立認證模組
  - 檔案: src/app/auth/auth.module.ts
  - 檔案: src/app/auth/login/login.component.ts
  - 檔案: src/app/auth/register/register.component.ts
  - 檔案: src/app/auth/services/auth.service.ts
  - 建立登入和註冊元件
  - 實作 AuthService
  - 目的: 提供用戶認證功能

- [ ] 8. 建立 OTP 模組
  - 檔案: src/app/otp/otp.module.ts
  - 檔案: src/app/otp/otp-verification/otp-verification.component.ts
  - 檔案: src/app/otp/qr-setup/qr-setup.component.ts
  - 檔案: src/app/otp/device-management/device-management.component.ts
  - 檔案: src/app/otp/services/otp.service.ts
  - 檔案: src/app/otp/services/qr-code.service.ts
  - 建立 OTP 生成和驗證元件
  - 實作 OTPService 和 QRCodeService
  - 目的: 提供 OTP 功能

- [ ] 9. 建立儀表板模組
  - 檔案: src/app/dashboard/dashboard.module.ts
  - 檔案: src/app/dashboard/dashboard.component.ts
  - 檔案: src/app/dashboard/profile/profile.component.ts
  - 檔案: src/app/dashboard/backup-codes/backup-codes.component.ts
  - 建立用戶儀表板元件
  - 顯示 OTP 使用統計和設定
  - 目的: 提供用戶管理介面
