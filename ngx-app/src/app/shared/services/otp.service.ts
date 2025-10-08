import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import {
  BindDeviceRequest,
  BindDeviceResponse,
  VerifyOTPRequest,
  VerifyOTPResponse,
  DeviceInfo,
  RecoveryCodesResponse
} from '../types/otp';

/**
 * OTP 服務 - 封裝與後端 API 的互動
 */
@Injectable({
  providedIn: 'root'
})
export class OtpService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5127/api';

  /**
   * 綁定新裝置 - 產生 QR Code
   */
  bindDevice(request: BindDeviceRequest): Observable<BindDeviceResponse> {
    return this.http.post<BindDeviceResponse>(`${this.baseUrl}/OTP/devices/bind`, request)
      .pipe(
        catchError(this.handleError)
      );
  }

  /**
   * 驗證裝置設定 - 初次綁定後驗證
   */
  verifySetup(request: VerifyOTPRequest): Observable<VerifyOTPResponse> {
    return this.http.post<VerifyOTPResponse>(`${this.baseUrl}/OTP/devices/verify-setup`, request)
      .pipe(
        catchError(this.handleError)
      );
  }

  /**
   * 驗證 OTP 代碼
   */
  verifyOTP(request: VerifyOTPRequest): Observable<VerifyOTPResponse> {
    return this.http.post<VerifyOTPResponse>(`${this.baseUrl}/OTP/verify`, request)
      .pipe(
        catchError(this.handleError)
      );
  }

  /**
   * 取得使用者裝置清單
   */
  getUserDevices(userId: string): Observable<DeviceInfo[]> {
    return this.http.get<DeviceInfo[]>(`${this.baseUrl}/OTP/devices?userId=${userId}`)
      .pipe(
        catchError(this.handleError)
      );
  }

  /**
   * 刪除裝置
   */
  deleteDevice(userId: string, deviceId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/OTP/devices/${deviceId}?userId=${userId}`)
      .pipe(
        catchError(this.handleError)
      );
  }

  /**
   * 產生備援恢復碼
   */
  generateRecoveryCodes(userId: string, count: number = 10): Observable<RecoveryCodesResponse> {
    return this.http.post<RecoveryCodesResponse>(
      `${this.baseUrl}/OTP/devices/recovery-codes?userId=${userId}&count=${count}`,
      {}
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * 錯誤處理
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = '發生未知錯誤';

    if (error.error instanceof ErrorEvent) {
      // 客戶端錯誤
      errorMessage = `客戶端錯誤: ${error.error.message}`;
    } else {
      // 伺服器錯誤
      if (error.status === 0) {
        errorMessage = '無法連接到伺服器';
      } else if (error.error?.errorMessage) {
        errorMessage = error.error.errorMessage;
      } else if (error.error?.message) {
        errorMessage = error.error.message;
      } else {
        errorMessage = `伺服器錯誤: ${error.status} ${error.statusText}`;
      }
    }

    console.error('OTP Service 錯誤:', error);
    return throwError(() => new Error(errorMessage));
  }
}
