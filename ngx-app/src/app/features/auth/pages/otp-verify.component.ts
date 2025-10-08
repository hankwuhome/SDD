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
 * OTP 驗證頁面 - 日常登入時的 OTP 驗證
 */
@Component({
  selector: 'app-otp-verify',
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
      <mat-card class="otp-verify-card">
        <mat-card-header>
          <mat-card-title class="text-center">
            <mat-icon class="mr-2">lock</mat-icon>
            雙因子驗證
          </mat-card-title>
          <mat-card-subtitle class="text-center">
            請輸入您的驗證器應用程式顯示的代碼
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="verifyForm" (ngSubmit)="onVerifyOTP()">
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

            <!-- 剩餘嘗試次數顯示 -->
            <div *ngIf="remainingAttempts() < 3 && remainingAttempts() > 0"
                 class="mb-4 p-3 bg-orange-50 border border-orange-200 rounded-lg">
              <div class="flex items-center text-orange-700">
                <mat-icon class="mr-2 text-orange-600">warning</mat-icon>
                <span class="text-sm">
                  剩餘嘗試次數: {{ remainingAttempts() }} 次
                </span>
              </div>
            </div>

            <div class="action-buttons space-y-3">
              <button mat-raised-button color="primary" type="submit"
                      class="w-full py-3"
                      [disabled]="verifyForm.invalid || loadingState().isLoading">
                <mat-spinner diameter="20" *ngIf="loadingState().isLoading"></mat-spinner>
                <span *ngIf="!loadingState().isLoading">驗證登入</span>
                <span *ngIf="loadingState().isLoading">驗證中...</span>
              </button>

              <button mat-stroked-button
                      type="button"
                      (click)="goToDeviceManagement()"
                      class="w-full">
                管理裝置
              </button>
            </div>
          </form>

          <!-- 成功狀態 -->
          <div *ngIf="isVerified()" class="text-center mt-6">
            <div class="success-message p-4 bg-green-50 border border-green-200 rounded-lg">
              <mat-icon class="text-green-600 text-4xl mb-2">check_circle</mat-icon>
              <h3 class="text-green-800 font-semibold">驗證成功！</h3>
              <p class="text-green-700 text-sm mt-2">您已成功通過雙因子驗證</p>
            </div>

            <button mat-raised-button color="primary"
                    (click)="goToHome()"
                    class="w-full mt-4">
              進入系統
            </button>
          </div>

          <!-- 說明文字 -->
          <div class="instructions mt-6 p-4 bg-blue-50 rounded-lg">
            <h4 class="font-semibold text-blue-800 mb-2">
              <mat-icon class="mr-1 text-blue-600">info</mat-icon>
              使用說明
            </h4>
            <ul class="text-sm text-blue-700 space-y-1">
              <li>• 開啟您的驗證器應用程式</li>
              <li>• 找到對應的帳號項目</li>
              <li>• 輸入顯示的 6 位數代碼</li>
              <li>• 如果代碼過期，請等待新代碼生成</li>
            </ul>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .otp-verify-card {
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

    .success-message {
      border-left: 4px solid #10b981;
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
export class OtpVerifyComponent implements OnInit {
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

  /** 剩餘嘗試次數 */
  remainingAttempts = signal<number>(3);

  /** 是否已驗證成功 */
  isVerified = signal<boolean>(false);

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
   * 驗證 OTP
   */
  onVerifyOTP(): void {
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

    this.otpService.verifyOTP(request).subscribe({
      next: (response) => {
        this.loadingState.set({
          isLoading: false,
          hasError: false,
          errorMessage: ''
        });

        this.remainingAttempts.set(response.remainingAttempts);

        if (response.success) {
          this.isVerified.set(true);
          this.snackBar.open('OTP 驗證成功！', '關閉', {
            duration: 3000,
            panelClass: ['success-snackbar']
          });
        } else {
          this.handleError(response.errorMessage || 'OTP 驗證失敗');
          // 清空輸入框
          this.verifyForm.get('code')?.setValue('');
        }
      },
      error: (error) => {
        this.handleError(error.message);
        this.verifyForm.get('code')?.setValue('');
      }
    });
  }

  /**
   * 前往首頁
   */
  goToHome(): void {
    this.router.navigate(['/home']);
  }

  /**
   * 前往裝置管理
   */
  goToDeviceManagement(): void {
    this.router.navigate(['/devices/manage'], {
      queryParams: { accountId: this.accountId() }
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
