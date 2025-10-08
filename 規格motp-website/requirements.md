# Requirements Document

## Introduction

建置 MOTP（Mobile One-Time Password）網站，提供使用者以行動裝置產生一次性密碼（OTP，One-Time Password）與綁定裝置管理，支援登入驗證整合與備援復原流程，提升企業與個人應用的多因子驗證（MFA，Multi-Factor Authentication）安全性。

## Alignment with Product Vision

- 強化帳號安全與信任等級，降低密碼外洩風險。
- 以簡潔操作與可存取性提升大眾採用率。
- 模組化與可延展，便於整合不同身分提供者（IdP，Identity Provider）。

## Requirements

### Requirement 1: 註冊與裝置綁定

**User Story:** 作為首次使用者，我希望通過帳號/密驗證後，用 Microsoft Authenticator 或 Google Authenticator 掃描網站產生的 QR Code，完成綁定行動裝置，
之後登入網站時輸入帳號/密碼，再輸入MOTP密碼即可完成登入驗證

#### Acceptance Criteria

1. WHEN 使用者完成帳號登入後進入「裝置綁定」頁 THEN 系統 SHALL 顯示 QR Code。
2. WHEN 使用者以行動 App 掃描 QR Code THEN 系統 SHALL 驗證一次綁定請求並回應成功或失敗。
3. IF 已綁定裝置超過上限（預設 2 台） THEN 系統 SHALL 阻擋並提示解除既有裝置後再綁定。

### Requirement 2: MOTP 產生與驗證

**User Story:** 作為已綁定裝置使用者，我希望能在網站上檢視即時 OTP 狀態並於登入時驗證，以確保安全性。
**User Story:** 支援 Microsoft Authenticator 及 Google Authenticator

#### Acceptance Criteria

1. WHEN 使用者於登入流程輸入 OTP THEN 系統 SHALL 依 TOTP（RFC 6238）驗證並允許/拒絕登入。
2. IF 時間偏移超過允許視窗（例如 ±30s 兩個 window） THEN 系統 SHALL 拒絕並提示同步時間。
3. WHEN 使用者請求重送或重試 THEN 系統 SHALL 記錄審計日誌，包含 IP、時間、結果。

### Requirement 3: 裝置管理與備援

**User Story:** 作為使用者，我希望能在網站上查看已綁定裝置並產生備援恢復碼，以便遺失手機時可復原。

#### Acceptance Criteria

1. WHEN 使用者進入「裝置管理」 THEN 系統 SHALL 顯示已綁定清單（暱稱、綁定時間、最近使用）。
2. WHEN 使用者產生備援恢復碼 THEN 系統 SHALL 顯示 10 組一次性恢復碼並要求二次確認後下載/列印。
3. WHEN 使用者解除某裝置綁定 THEN 系統 SHALL 立即使該裝置的 OTP 失效。

### Requirement 4: 系統整合與權限

**User Story:** 作為系統管理員，我希望可將 MOTP 與既有身分系統（如 OAuth 2.0 / OIDC）整合，並管理角色權限。

#### Acceptance Criteria

1. WHEN 後端服務需要驗證請求 THEN 系統 SHALL 以 OIDC 取得 ID Token 並附帶 OTP 驗證結果（AMR/ACR）。
2. WHEN 管理員指派角色（admin/user） THEN 系統 SHALL 控制頁面與 API 的存取。
3. IF 整合失敗（設定錯誤或密鑰不符） THEN 系統 SHALL 回傳清楚錯誤與修復指引。

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: 每個檔案職責單一、清楚。
- **Modular Design**: 元件、工具、服務可重用、低耦合。
- **Dependency Management**: 最小化跨模組相依。
- **Clear Interfaces**: 以明確介面隔離層次與責任。

### Performance
- 初次載入 ≤ 200KB JS（壓縮後，不含字型/圖片）。
- OTP 驗證 API P95 延遲 ≤ 150ms（同區域）。

### Security
- 前後端全程 HTTPS；關鍵金鑰採 KMS/環境變數管理。
- 敏感資料不落地；避免在前端儲存長期憑證。
- 防止重放攻擊、時序攻擊與常見 OWASP Top 10 風險。

### Reliability
- 後端 OTP 驗證服務可用性 ≥ 99.9%。
- 有系統監控與告警；異常自動化復原流程。

### Usability
- 行動優先版面，支援深色模式。
- 可存取性達到 WCAG 2.1 AA。 