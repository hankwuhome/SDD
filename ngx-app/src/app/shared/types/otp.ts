/**
 * OTP 相關型別定義
 */

/** 裝置綁定請求 */
export interface BindDeviceRequest {
  /** 帳號（Email） */
  accountId: string;
  /** 密碼 */
  password: string;
  /** 裝置名稱 */
  deviceName: string;
}

/** 裝置綁定回應 */
export interface BindDeviceResponse {
  /** QR Code 資料（Base64） */
  qrCodeData: string;
  /** 密鑰 */
  secretKey: string;
  /** OTP Auth URI */
  otpAuthUri: string;
  /** 是否成功 */
  success: boolean;
  /** 錯誤訊息 */
  errorMessage?: string;
}

/** OTP 驗證請求 */
export interface VerifyOTPRequest {
  /** 帳號（Email） */
  accountId: string;
  /** OTP 代碼 */
  code: string;
  /** 時間窗口容錯 */
  leewayWindows?: number;
  /** 客戶端時間（毫秒） */
  clientTimeMs?: number;
}

/** OTP 驗證回應 */
export interface VerifyOTPResponse {
  /** 是否成功 */
  success: boolean;
  /** 錯誤訊息 */
  errorMessage?: string;
  /** 剩餘嘗試次數 */
  remainingAttempts: number;
}

/** 裝置資訊 */
export interface DeviceInfo {
  /** 裝置 ID */
  id: string;
  /** 裝置名稱 */
  deviceName: string;
  /** 建立時間 */
  createdAt: string;
  /** 最後使用時間 */
  lastUsedAt?: string;
  /** 是否啟用 */
  isActive: boolean;
}

/** 備援恢復碼回應 */
export interface RecoveryCodesResponse {
  /** 恢復碼清單 */
  recoveryCodes: string[];
  /** 是否成功 */
  success: boolean;
  /** 錯誤訊息 */
  errorMessage?: string;
}

/** API 回應基礎型別 */
export interface ApiResponse<T = any> {
  /** 是否成功 */
  success: boolean;
  /** 資料 */
  data?: T;
  /** 錯誤訊息 */
  message?: string;
}

/** 載入狀態 */
export interface LoadingState {
  /** 是否載入中 */
  isLoading: boolean;
  /** 是否有錯誤 */
  hasError: boolean;
  /** 錯誤訊息 */
  errorMessage?: string;
}
