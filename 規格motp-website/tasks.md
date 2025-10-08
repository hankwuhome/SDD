# Tasks Document

- [x] 1. 建立前端路由與頁面骨架（Angular）
  - File: ngx-app/src/app/features/
  - 建立 `features/auth/pages/bind-device`、`features/auth/pages/otp-verify`、`features/devices/pages/manage-devices` 之 standalone components 與對應路由。
  - Purpose: 提供綁定、驗證、管理頁面骨架。
  - _Leverage: Angular Router, Angular Material, TailwindCSS_
  - _Requirements: Requirement 1, Requirement 2, Requirement 3_
  - _Prompt: Role: Frontend Angular Developer | Task: Scaffold standalone components and routes for bind-device, otp-verify, manage-devices, using Angular Material form controls and Tailwind for layout. Ensure accessibility (WCAG 2.1 AA) and mobile-first responsive design. | Restrictions: Use standalone components, no class-based services unless necessary, keep files small and modular. | Success: Routes reachable, toolbar present, pages render with placeholders and pass build._

- [x] 2. 建立前端 OtpService 與型別（Angular）
  - File: ngx-app/src/app/shared/services/otp.service.ts; ngx-app/src/app/shared/types/otp.ts
  - 封裝 API：POST `/api/devices/bind`、POST `/api/otp/verify`、GET `/api/devices`、POST `/api/devices/recovery-codes`、DELETE `/api/devices/:id`。
  - Purpose: 提供前端與後端互動介面與型別安全。
  - _Leverage: Angular HttpClient, RxJS_
  - _Requirements: Requirement 1, Requirement 2, Requirement 3_
  - _Prompt: Role: TypeScript Developer | Task: Implement OtpService with typed methods returning Observables, define interfaces for requests/responses. Handle errors with RxJS operators and surface friendly messages. | Restrictions: Strong typing, no any, keep functions small, add minimal unit tests for method signatures. | Success: Service compiles, methods callable from pages, basic tests green._

- [x] 3. 實作前端頁面功能（Angular）
  - File: ngx-app/src/app/features/**
  - bind-device：顯示 Base32 secret 與 QRCode（otpauth URI），送出綁定請求。
  - otp-verify：輸入 6 碼 OTP，送出驗證，顯示結果與錯誤。
  - manage-devices：列出裝置、解除綁定、產生並下載備援恢復碼（10 組）。
  - Purpose: 完成核心使用流程。
  - _Leverage: OtpService, Angular Material Dialog/Table/FormField, Tailwind utility classes_
  - _Requirements: Requirement 1, Requirement 2, Requirement 3_
  - _Prompt: Role: Frontend Engineer | Task: Wire up UI with OtpService; implement forms with validation; add dialogs for confirmations and recovery code display; ensure i18n-ready strings. | Restrictions: Avoid heavy state libs; use Angular signals or component state; handle loading/disabled states. | Success: Happy-path flows work against mock server; UI responsive and accessible._

- [x] 4. 建立後端專案骨架（.NET 8 WebAPI）
  - File: BackEnd/
  - 使用 `dotnet new webapi`，分層：`Controllers/`, `Services/`, `Repositories/`, `Models/`。
  - Purpose: 提供 OTP 驗證 API 基礎。
  - _Leverage: .NET 8 WebAPI minimal hosting, Dependency Injection, Logging_
  - _Requirements: Requirement 2, Requirement 3, Requirement 4_
  - _Prompt: Role: Backend .NET Developer | Task: Create WebAPI project with clean layering, add basic health endpoint, wire DI containers. | Restrictions: Use appsettings for configs; no secrets in repo; add Swagger. | Success: Project builds and runs; Swagger UI available._

- [x] 5. 實作 OTP 與裝置 API（.NET 8 WebAPI）
  - File: BackEnd/Controllers/, BackEnd/Services/, BackEnd/Repositories/
  - Endpoints：
    - POST `/api/devices/bind`（產生 secret、記錄裝置、回 QR 設定資料）
    - GET `/api/devices`（列出裝置）
    - DELETE `/api/devices/{id}`（解除綁定）
    - POST `/api/devices/recovery-codes`（產生 10 組一次性恢復碼）
    - POST `/api/otp/verify`（驗證 TOTP，支援 time-window 容忍）
  - Purpose: 覆蓋綁定、管理、驗證全流程。
  - _Leverage: TOTP libraries (e.g., Otp.NET), Repository pattern_
  - _Requirements: Requirement 1, Requirement 2, Requirement 3_
  - _Prompt: Role: Backend Engineer (.NET) | Task: Implement services and controllers; generate Base32 secret; build otpauth URI (issuer/account); use Otp.NET for TOTP verify with ±1 window; add structured logs and audit entries. | Restrictions: Input validation via FluentValidation or data annotations; return proper HTTP codes; no sensitive data in logs. | Success: Integration tested from frontend; unit tests for time-window and edge cases._

- [ ] 6. OIDC 整合（回傳 OTP 結果於 AMR/ACR）
  - File: BackEnd/Services/Auth/
  - 登入流程中將 OTP 驗證結果寫入 Claims（AMR/ACR），配合前端顯示與 API 保護。
  - Purpose: 與現有身分提供者整合。
  - _Leverage: Microsoft.IdentityModel.Tokens, OpenIddict/IdentityServer（依環境）_
  - _Requirements: Requirement 4_
  - _Prompt: Role: Identity Engineer | Task: Integrate OTP verification result into claims; enforce policy on protected endpoints. | Restrictions: Do not expose secrets; configurable issuers; robust error messages. | Success: Protected API requires OTP-verified context; tokens含對應 AMR/ACR._

- [ ] 7. 安全與設定管理
  - File: BackEnd/appsettings.json, Key management
  - Purpose: 以環境變數/KMS 管理密鑰；設定 CORS、Rate Limit、CSP。
  - _Leverage: ASP.NET Rate Limiting, Data Protection, Secret Manager_
  - _Requirements: Security NFR_
  - _Prompt: Role: Security Engineer | Task: Configure HTTPS-only, CORS allowlist, rate limiting, secure headers; store secrets securely (no plaintext). | Restrictions: No secrets in repo; sensible defaults. | Success: 安全掃描通過，設定覆蓋生產/開發環境._

- [ ] 8. 測試與自動化
  - File: BackEnd/tests/, ngx-app/src/app/**
  - 前端單元測試（關鍵服務與表單），後端單元與整合測試（OTP 驗證邏輯、API）。
  - Purpose: 確保可靠性與避免回歸。
  - _Leverage: Angular testing utilities, xUnit/NUnit, Swagger-generated clients (optional)_
  - _Requirements: Testing Strategy_
  - _Prompt: Role: QA Engineer | Task: Write unit/integration tests; include time drift, invalid code, binding limit cases; add basic E2E happy path. | Restrictions: Avoid flaky; deterministic clocks/mocks. | Success: CI 內測試通過，關鍵路徑覆蓋充分._ 