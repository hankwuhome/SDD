import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';

import { OtpService } from '../../../shared/services/otp.service';
import { BindDeviceRequest, LoadingState } from '../../../shared/types/otp';

/**
 * 裝置綁定頁面 - 產生 QR Code 供使用者掃描
 */
@Component({
  selector: 'app-bind-device',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatIconModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>
          <mat-icon>security</mat-icon>
          綁定 TOTP 裝置
        </mat-card-title>
        <mat-card-subtitle>
          設定雙因子驗證以保護您的帳戶
        </mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        <!-- 綁定表單 -->
        <form [formGroup]="bindForm" (ngSubmit)="onBindDevice()" *ngIf="!qrCodeData()">
          <mat-form-field appearance="outline">
            <mat-label>帳號 (Email)</mat-label>
            <input matInput type="email" formControlName="accountId"
                   placeholder="請輸入您的 Email 帳號">
            <mat-error *ngIf="bindForm.get('accountId')?.hasError('required')">
              請輸入帳號
            </mat-error>
            <mat-error *ngIf="bindForm.get('accountId')?.hasError('email')">
              請輸入有效的 Email 地址
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>密碼</mat-label>
            <input matInput type="password" formControlName="password"
                   placeholder="請輸入密碼">
            <mat-error *ngIf="bindForm.get('password')?.hasError('required')">
              請輸入密碼
            </mat-error>
            <mat-error *ngIf="bindForm.get('password')?.hasError('minlength')">
              密碼至少需要 6 個字元
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>裝置名稱</mat-label>
            <input matInput formControlName="deviceName"
                   placeholder="例如：我的手機">
            <mat-error *ngIf="bindForm.get('deviceName')?.hasError('required')">
              請輸入裝置名稱
            </mat-error>
          </mat-form-field>

          <button mat-raised-button color="primary" type="submit"
                  [disabled]="bindForm.invalid || loadingState().isLoading">
            <mat-spinner diameter="20" *ngIf="loadingState().isLoading"></mat-spinner>
            <span *ngIf="!loadingState().isLoading">產生 QR Code</span>
            <span *ngIf="loadingState().isLoading">處理中...</span>
          </button>
        </form>

        <!-- QR Code 顯示 -->
        <div *ngIf="qrCodeData()" class="text-center">
          <h3>請使用驗證器應用程式掃描 QR Code</h3>

          <div class="qr-code-container">
            <img [src]="'data:image/png;base64,' + qrCodeData()"
                 alt="QR Code">
          </div>

          <div class="info-section">
            <h4><mat-icon>info</mat-icon> 設定步驟：</h4>
            <ol>
              <li>下載 Google Authenticator 或 Microsoft Authenticator</li>
              <li>開啟應用程式並掃描上方 QR Code</li>
              <li>點擊下方按鈕進行驗證設定</li>
            </ol>
          </div>

          <div class="margin-top">
            <button mat-raised-button color="primary"
                    (click)="goToVerifySetup()">
              驗證設定
            </button>

            <button mat-stroked-button
                    (click)="resetForm()"
                    class="margin-top">
              重新產生
            </button>
          </div>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    mat-card {
      margin: 20px auto;
      max-width: 500px;
    }

    mat-card-title {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .info-section ol {
      text-align: left;
      padding-left: 20px;
    }

    .info-section ol li {
      margin-bottom: 8px;
    }

    .info-section h4 {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
    }
  `]
})
export class BindDeviceComponent {
  private readonly fb = inject(FormBuilder);
  private readonly otpService = inject(OtpService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  /** 綁定表單 */
  bindForm: FormGroup = this.fb.group({
    accountId: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    deviceName: ['', [Validators.required]]
  });

  /** QR Code 資料 */
  qrCodeData = signal<string>('');

  /** 載入狀態 */
  loadingState = signal<LoadingState>({
    isLoading: false,
    hasError: false,
    errorMessage: ''
  });

  /**
   * 綁定裝置
   */
  onBindDevice(): void {
    if (this.bindForm.invalid) {
      return;
    }

    this.loadingState.set({
      isLoading: true,
      hasError: false,
      errorMessage: ''
    });

    const request: BindDeviceRequest = this.bindForm.value;

    this.otpService.bindDevice(request).subscribe({
      next: (response) => {
        this.loadingState.set({
          isLoading: false,
          hasError: false,
          errorMessage: ''
        });

        if (response.success) {
          this.qrCodeData.set(response.qrCodeData);
          this.snackBar.open('QR Code 產生成功！請掃描設定驗證器', '關閉', {
            duration: 5000,
            panelClass: ['success-snackbar']
          });
        } else {
          this.handleError(response.errorMessage || '綁定失敗');
        }
      },
      error: (error) => {
        this.handleError(error.message);
      }
    });
  }

  /**
   * 前往驗證設定頁面
   */
  goToVerifySetup(): void {
    const accountId = this.bindForm.get('accountId')?.value;
    this.router.navigate(['/auth/verify-setup'], {
      queryParams: { accountId }
    });
  }

  /**
   * 重置表單
   */
  resetForm(): void {
    this.qrCodeData.set('');
    this.bindForm.reset();
    this.loadingState.set({
      isLoading: false,
      hasError: false,
      errorMessage: ''
    });
  }

  /**
   * 錯誤處理
   */
  private handleError(errorMessage: string): void {
    this.loadingState.set({
      isLoading: false,
      hasError: true,
      errorMessage
    });

    this.snackBar.open(errorMessage, '關閉', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
}
