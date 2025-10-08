# 認證 API 測試流程說明

## 🚀 快速開始

### 1. 啟動 API 服務
```bash
cd MOTPDualAuthWebsite.API
dotnet run
```

API 將在 `https://localhost:7062` 或 `http://localhost:5062` 啟動

### 2. 開啟 Swagger UI
瀏覽器開啟：`https://localhost:7062` 或 `http://localhost:5062`

---

## 📋 完整測試流程

### 階段一：準備測試資料

#### 1. 建立測試使用者
**端點**：`POST /api/otp/bind-device`

**測試資料**：
```json
{
  "accountId": "test@example.com",
  "password": "123456",
  "deviceName": "測試手機"
}
```

**預期結果**：
- 狀態碼：200
- 回應包含：QR Code 資料、SecretKey、OtpAuthUri
- 資料庫建立使用者和 OTP 裝置記錄

---

### 階段二：認證流程測試

#### 2. 使用者登入（帳號密碼）
**端點**：`POST /api/auth/login`

**測試資料**：
```json
{
  "email": "test@example.com",
  "password": "123456",
  "rememberMe": false
}
```

**預期結果**：
- **有 TOTP 裝置**：
  ```json
  {
    "success": true,
    "requiresTOTP": true,
    "userId": "guid-string"
  }
  ```
- **無 TOTP 裝置**：
  ```json
  {
    "success": true,
    "requiresTOTP": false,
    "token": "jwt-token-string",
    "user": {
      "id": "guid",
      "email": "test@example.com",
      "isEmailVerified": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "lastLoginAt": "2024-01-01T00:00:00Z",
      "hasTOTPEnabled": false,
      "activeDevicesCount": 0
    }
  }ㄑ
  ```

#### 3. TOTP 驗證（如果需要）
**端點**：`POST /api/auth/verify-totp`

**測試資料**：
```json
{
  "userId": "從登入(/api/auth/login)回應取得的 userId",
  "code": "123456", //TOTP
  "isBackupCode": false
}
```

**TOTP 驗證碼取得方式**：
1. 使用 Google Authenticator 掃描 QR Code
2. 或使用 `POST /api/otp/validate` 端點測試
3. 或使用備用驗證碼（設定 `isBackupCode: true`）

**預期結果**：
```json
{
  "success": true,
  "token": "jwt-token-string",
  "user": {
    "id": "guid",
    "email": "test@example.com",
    "isEmailVerified": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastLoginAt": "2024-01-01T00:00:00Z",
    "hasTOTPEnabled": true,
    "activeDevicesCount": 1
  }
}
```

---

### 階段三：認證後功能測試

#### 4. 設定 JWT Token
從登入或 TOTP 驗證取得 Token 後：

**在 Swagger UI 中**：
1. 點擊右上角 "Authorize" 按鈕
2. 輸入：`Bearer {your-jwt-token}`
3. 點擊 "Authorize"

#### 5. 取得使用者資訊
**端點**：`GET /api/auth/profile`

**需要認證**：是（Bearer Token）

**預期結果**：
```json
{
  "user": {
    "id": "guid",
    "email": "test@example.com",
    "isEmailVerified": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastLoginAt": "2024-01-01T00:00:00Z",
    "hasTOTPEnabled": true,
    "activeDevicesCount": 1
  },
  "activeSessions": [
    {
      "id": "session-guid",
      "ipAddress": "127.0.0.1",
      "userAgent": "Mozilla/5.0...",
      "loginAt": "2024-01-01T00:00:00Z",
      "expiresAt": "2024-01-01T08:00:00Z",
      "isCurrent": true
    }
  ]
}
```

#### 6. 使用者登出
**端點**：`POST /api/auth/logout`

**需要認證**：是（Bearer Token）

**測試資料**：
```json
{
  "logoutAllDevices": false
}
```

**預期結果**：
```json
{
  "success": true,
  "message": "登出成功"
}
```

**全部裝置登出**：
```json
{
  "logoutAllDevices": true
}
```

---

## 🔍 錯誤情境測試

### 1. 登入失敗測試

#### 錯誤密碼
```json
{
  "email": "test@example.com",
  "password": "wrong-password",
  "rememberMe": false
}
```

**預期結果**：
- 狀態碼：401
- 回應：
```json
{
  "success": false,
  "errorMessage": "帳號或密碼錯誤",
  "remainingAttempts": 4
}
```

#### 帳號鎖定
連續失敗 5 次後：

**預期結果**：
- 狀態碼：423
- 回應：
```json
{
  "success": false,
  "errorMessage": "帳號已被鎖定，請於 2024-01-01 12:15 後再試",
  "lockedUntil": "2024-01-01T12:15:00Z"
}
```

### 2. TOTP 驗證失敗

#### 錯誤驗證碼
```json
{
  "userId": "valid-user-id",
  "code": "000000",
  "isBackupCode": false
}
```

**預期結果**：
- 狀態碼：401
- 回應：
```json
{
  "success": false,
  "errorMessage": "TOTP 驗證碼錯誤或已過期",
  "remainingAttempts": 2
}
```

### 3. 未認證存取

存取需要認證的端點但未提供 Token：

**預期結果**：
- 狀態碼：401
- 標準的 401 Unauthorized 回應

---

## 📊 測試檢查清單

### ✅ 基本功能
- [ ] 使用者註冊（透過 OTP 裝置綁定）
- [ ] 帳號密碼登入
- [ ] TOTP 驗證
- [ ] 取得使用者資訊
- [ ] 使用者登出
- [ ] 撤銷指定會話

### ✅ 安全功能
- [ ] 密碼錯誤處理
- [ ] 帳號鎖定機制
- [ ] TOTP 驗證碼錯誤處理
- [ ] JWT Token 過期處理
- [ ] 未認證存取拒絕

### ✅ 會話管理
- [ ] 單一裝置登出
- [ ] 全部裝置登出
- [ ] 會話資訊追蹤
- [ ] 過期會話清理

---

## 🛠️ 測試工具

### 1. Swagger UI
- **優點**：視覺化介面、自動文檔、JWT 認證支援
- **使用**：直接在瀏覽器中測試所有端點

### 2. Postman
- **優點**：功能強大、可儲存請求、環境變數支援
- **設定**：
  1. 建立環境變數：`baseUrl = https://localhost:7062`
  2. 建立環境變數：`token = {從登入取得的 JWT}`
  3. 在需要認證的請求中設定 Header：`Authorization: Bearer {{token}}`

### 3. curl 命令
```bash
# 登入
curl -X POST "https://localhost:7062/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "123456",
    "rememberMe": false
  }'

# 取得使用者資訊
curl -X GET "https://localhost:7062/api/auth/profile" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 📝 測試資料範例

### 建立多個測試使用者
```json
// 使用者 1 - 有 TOTP
{
  "accountId": "user1@test.com",
  "password": "password123",
  "deviceName": "iPhone 13"
}

// 使用者 2 - 無 TOTP（先註冊後刪除裝置）
{
  "accountId": "user2@test.com",
  "password": "password456",
  "deviceName": "Samsung Galaxy"
}
```

### JWT Token 測試
- **短期 Token**：`rememberMe: false` (8小時)
- **長期 Token**：`rememberMe: true` (30天)

---

## ⚠️ 注意事項

1. **SSL 憑證**：開發環境可能需要接受自簽憑證
2. **資料庫連線**：確保 SQL Server 正在運行
3. **時間同步**：TOTP 驗證需要系統時間準確
4. **日誌檢查**：測試時可查看控制台日誌了解詳細資訊
5. **資料清理**：測試後可重置資料庫或清理測試資料

---

## 🔧 故障排除

### 常見問題

1. **API 無法啟動**
   - 檢查 SQL Server 連線
   - 確認連接字串正確
   - 執行 `dotnet ef database update`

2. **JWT Token 無效**
   - 檢查 Token 是否過期
   - 確認 Authorization Header 格式正確
   - 驗證 JWT 設定參數

3. **TOTP 驗證失敗**
   - 確認系統時間準確
   - 檢查 TOTP 參數設定
   - 使用備用驗證碼測試

4. **資料庫錯誤**
   - 檢查資料庫連線
   - 確認遷移已應用
   - 查看詳細錯誤日誌

---

## 📈 效能測試

### 建議的負載測試
- 併發登入請求
- 大量 TOTP 驗證
- 長時間會話測試
- 記憶體使用監控

這個測試流程涵蓋了完整的認證系統功能，確保所有功能都能正常運作！ 