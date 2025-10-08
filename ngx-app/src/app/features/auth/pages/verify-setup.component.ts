import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';

import { OtpService } from '../../../shared/services/otp.service';
import { VerifyOTPRequest, LoadingState } from '../../../shared/types/otp';

/**
 * 驗證設定頁面 - 驗證 TOTP 設定是否正確
 */
@Component({
  selector: 'app-verify-setup',
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
    <div class="container mx-auto px-4 py-8 max-w-md">
      <mat-card class="verify-setup-card">
        <mat-card-header>
          <mat-card-title class="text-center">
            <mat-icon class="mr-2">verified_user</mat-icon>
            驗證設定
          </mat-card-title>
          <mat-card-subtitle class="text-center">
            請輸入驗證器應用程式顯示的 6 位數代碼
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="verifyForm" (ngSubmit)="onVerifySetup()">
            <div class="mb-4">
              <p class="text-sm text-gray-600 mb-2">
                帳號: <strong>{{ accountId() }}</strong>
              </p>
            </div>

            <mat-form-field appearance="outline" class="w-full mb-6">
              <mat-label>驗證碼</mat-label>
              <input matInput
                     formControlName="code"
                     placeholder="請輸入 6 位數驗證碼"
                     maxlength="6"
                     class="text-center text-2xl tracking-widest">
              <mat-error *ngIf="verifyForm.get('code')?.hasError('required')">
                請輸入驗證碼
              </mat-error>
              <mat-error *ngIf="verifyForm.get('code')?.hasError('pattern')">
                請輸入 6 位數字
              </mat-error>
            </mat-form-field>

            <div class="action-buttons space-y-3">
              <button mat-raised-button color="primary" type="submit"
                      class="w-full py-3"
                      [disabled]="verifyForm.invalid || loadingState().isLoading">
                <mat-spinner diameter="20" *ngIf="loadingState().isLoading"></mat-spinner>
                <span *ngIf="!loadingState().isLoading">驗證設定</span>
                <span *ngIf="loadingState().isLoading">驗證中...</span>
              </button>

              <button mat-stroked-button
                      type="button"
                      (click)="goBack()"
                      class="w-full">
                返回重新產生 QR Code
              </button>
            </div>
          </form>

          <!-- 說明文字 -->
          <div class="instructions mt-6 p-4 bg-blue-50 rounded-lg">
            <h4 class="font-semibold text-blue-800 mb-2">
              <mat-icon class="mr-1 text-blue-600">info</mat-icon>
              驗證說明
            </h4>
            <ul class="text-sm text-blue-700 space-y-1">
              <li>• 開啟您的驗證器應用程式</li>
              <li>• 找到剛才掃描的帳號項目</li>
              <li>• 輸入顯示的 6 位數代碼</li>
              <li>• 代碼每 30 秒會自動更新</li>
            </ul>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .verify-setup-card {
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }

    .action-buttons button {
      margin-bottom: 8px;
    }

    mat-card-title {
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
    }

    .instructions {
      border-left: 4px solid #3b82f6;
    }

    .instructions ul {
      list-style: none;
      padding-left: 0;
    }
  `]
})
export class VerifySetupComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly otpService = inject(OtpService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /** 驗證表單 */
  verifyForm: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  /** 帳號 ID */
  accountId = signal<string>('');

  /** 載入狀態 */
  loadingState = signal<LoadingState>({
    isLoading: false,
    hasError: false,
    errorMessage: ''
  });

  ngOnInit(): void {
    // 從查詢參數取得帳號 ID
    this.route.queryParams.subscribe(params => {
      const accountId = params['accountId'];
      if (accountId) {
        this.accountId.set(accountId);
      } else {
        // 如果沒有帳號 ID，返回綁定頁面
        this.router.navigate(['/auth/bind-device']);
      }
    });
  }

  /**
   * 驗證設定
   */
  onVerifySetup(): void {
    if (this.verifyForm.invalid) {
      return;
    }

    this.loadingState.set({
      isLoading: true,
      hasError: false,
      errorMessage: ''
    });

    const request: VerifyOTPRequest = {
      accountId: this.accountId(),
      code: this.verifyForm.get('code')?.value
    };

    this.otpService.verifySetup(request).subscribe({
      next: (response) => {
        this.loadingState.set({
          isLoading: false,
          hasError: false,
          errorMessage: ''
        });

        if (response.success) {
          this.snackBar.open('設定驗證成功！TOTP 裝置已啟用', '關閉', {
            duration: 5000,
            panelClass: ['success-snackbar']
          });

          // 前往 OTP 驗證頁面
          this.router.navigate(['/auth/otp-verify'], {
            queryParams: { accountId: this.accountId() }
          });
        } else {
          this.handleError(response.errorMessage || '驗證失敗，請檢查驗證碼是否正確');
        }
      },
      error: (error) => {
        this.handleError(error.message);
      }
    });
  }

  /**
   * 返回綁定頁面
   */
  goBack(): void {
    this.router.navigate(['/auth/bind-device']);
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
