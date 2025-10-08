# 🔐 MOTP Swagger 測試完整流程

## 📋 前置準備

### 1. 啟動 API 服務
```bash
cd MOTPDualAuthWebsite.API
dotnet run
```

### 2. 開啟 Swagger UI
- 在瀏覽器中訪問：`http://localhost:5127`
- 您會看到 MOTP Dual Auth Website API 的 Swagger 介面

## 🧪 完整測試流程

### 步驟 1：綁定新設備 📱

**API 端點：** `POST /api/OTP/bind-device`

1. 在 Swagger UI 中找到 `bind-device` 端點
2. 點擊展開該端點
3. 點擊 **「Try it out」** 按鈕
4. 在 Request body 中輸入以下測試資料：

```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "deviceName": "我的測試手機",
  "userEmail": "test@example.com"
}
```

5. 點擊 **「Execute」** 執行請求

**預期結果：**
- HTTP 狀態碼：200
- 回應包含 `qrCodeData`（Base64 編碼的 QR Code）
- 回應包含 `message` 顯示成功訊息

**📝 記錄重要資訊：**
- 保存 `userId`：`12345678-1234-1234-1234-123456789012`
- 保存 `deviceName`：`我的測試手機`
- 複製 `qrCodeData` 內容

---

### 步驟 2：設置 TOTP 應用程式 📲

1. **安裝 TOTP 應用程式**（選擇其中一個）：
   - Google Authenticator（推薦）
   - Microsoft Authenticator
   - Authy

2. **添加帳戶**：
   - 開啟 TOTP 應用程式
   - 選擇「掃描 QR Code」或「手動輸入」
   - 如果掃描：將步驟1的 `qrCodeData` 轉換為 QR Code 圖片
   - 如果手動輸入：使用密鑰（需要從 QR Code 中提取）

3. **驗證設置**：
   - 應用程式會顯示 6 位數驗證碼
   - 驗證碼每 30 秒更新一次

---

### 步驟 3：驗證設備設置 ✅

**API 端點：** `POST /api/OTP/verify-setup`

1. 找到 `verify-setup` 端點
2. 點擊 **「Try it out」**
3. 輸入以下資料：

```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "deviceName": "我的測試手機",
  "otpCode": "從TOTP應用程式取得的6位數驗證碼"
}
```

4. 點擊 **「Execute」**

**預期結果：**
- HTTP 狀態碼：200
- `isValid`: true
- 設備現在已啟用

---

### 步驟 4：測試 OTP 驗證 🔑

**API 端點：** `POST /api/OTP/verify`

1. 找到 `verify` 端點
2. 點擊 **「Try it out」**
3. 輸入測試資料：

```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "otpCode": "從TOTP應用程式取得的最新6位數驗證碼"
}
```

4. 點擊 **「Execute」**

**預期結果：**
- HTTP 狀態碼：200
- `isValid`: true
- 驗證成功

---

### 步驟 5：查看使用者設備 📋

**API 端點：** `GET /api/OTP/devices/{userId}`

1. 找到 `devices` 端點
2. 點擊 **「Try it out」**
3. 在 `userId` 參數中輸入：`12345678-1234-1234-1234-123456789012`
4. 點擊 **「Execute」**

**預期結果：**
- HTTP 狀態碼：200
- 回傳設備清單，包含剛才綁定的設備
- 顯示設備名稱、建立時間、最後使用時間等資訊

---

### 步驟 6：產生備用驗證碼 🆘

**API 端點：** `POST /api/OTP/generate-recovery-codes`

1. 找到 `generate-recovery-codes` 端點
2. 點擊 **「Try it out」**
3. 輸入測試資料：

```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "count": 10
}
```

4. 點擊 **「Execute」**

**預期結果：**
- HTTP 狀態碼：200
- 回傳 10 個備用驗證碼
- 每個驗證碼都是 8 位數的英數字組合

---

### 步驟 7：測試備用驗證碼 🔄

1. 複製其中一個備用驗證碼
2. 回到 `POST /api/OTP/verify` 端點
3. 使用備用驗證碼進行驗證：

```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "otpCode": "複製的備用驗證碼"
}
```

**預期結果：**
- 第一次使用：驗證成功
- 第二次使用同一個備用驗證碼：驗證失敗（一次性使用）

---

### 步驟 8：刪除設備（可選） 🗑️

**API 端點：** `DELETE /api/OTP/device/{userId}/{deviceId}`

1. 先從步驟 5 的回應中取得 `deviceId`
2. 找到 `device` DELETE 端點
3. 點擊 **「Try it out」**
4. 輸入參數：
   - `userId`: `12345678-1234-1234-1234-123456789012`
   - `deviceId`: 從步驟 5 取得的設備 ID
5. 點擊 **「Execute」**

**預期結果：**
- HTTP 狀態碼：200
- 設備已被刪除
- 再次查看設備清單會發現該設備已不存在

---

## 🔍 測試要點

### ✅ 成功指標
- 所有 API 都回傳 200 狀態碼
- QR Code 能被 TOTP 應用程式正確識別
- TOTP 驗證碼能正確驗證
- 備用驗證碼能正常使用且為一次性

### ⚠️ 常見問題

1. **OTP 驗證失敗**
   - 檢查時間同步（TOTP 基於時間）
   - 確保使用最新的驗證碼
   - 驗證碼有 30 秒有效期

2. **設備綁定失敗**
   - 檢查 GUID 格式是否正確
   - 確保設備名稱未重複
   - 檢查是否達到設備數量上限（2 個）

3. **API 無回應**
   - 確認 API 服務正在運行
   - 檢查端口 5127 是否可用
   - 檢查防火牆設定

---

## 📊 測試資料範例

### 測試使用者資料
```json
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "userEmail": "test@example.com"
}
```

### 測試設備資料
```json
{
  "deviceName": "iPhone 15",
  "deviceName2": "Android 手機"
}
```

---

## 🎯 進階測試場景

### 場景 1：多設備測試
1. 綁定第一個設備：「iPhone」
2. 綁定第二個設備：「Android」
3. 嘗試綁定第三個設備（應該失敗）
4. 使用兩個設備的驗證碼都能成功驗證

### 場景 2：錯誤處理測試
1. 使用錯誤的 GUID 格式
2. 使用過期的 OTP 驗證碼
3. 重複使用備用驗證碼
4. 刪除不存在的設備

### 場景 3：安全性測試
1. 使用他人的 userId 嘗試操作
2. 使用無效的 OTP 格式
3. 大量錯誤驗證嘗試

---

## 📱 推薦的 TOTP 應用程式

| 應用程式 | 平台 | 特色 |
|---------|------|------|
| Google Authenticator | iOS/Android | 簡單易用，廣泛支援 |
| Microsoft Authenticator | iOS/Android | 支援推播通知 |
| Authy | iOS/Android/Desktop | 雲端同步，多平台 |
| 1Password | 全平台 | 整合密碼管理 |

---

**🎉 完成測試後，您就成功驗證了 MOTP 雙因子驗證系統的完整功能！** 