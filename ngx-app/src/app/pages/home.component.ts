import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';

/**
 * 首頁組件 - 驗證成功後的歡迎頁面
 */
@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="container mx-auto px-4 py-8 max-w-2xl">
      <mat-card class="home-card">
        <mat-card-header>
          <mat-card-title class="text-center">
            <mat-icon class="mr-2 text-green-600">verified_user</mat-icon>
            歡迎使用 MOTP 系統
          </mat-card-title>
          <mat-card-subtitle class="text-center">
            您已成功通過雙因子驗證
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content class="text-center">
          <div class="success-message mb-6">
            <mat-icon class="success-icon">check_circle</mat-icon>
            <h2 class="text-2xl font-semibold text-green-800 mb-2">驗證成功！</h2>
            <p class="text-green-700">您的帳戶已受到雙因子驗證保護</p>
          </div>

          <div class="features-grid">
            <div class="feature-item">
              <mat-icon class="feature-icon">security</mat-icon>
              <h3>安全保護</h3>
              <p>您的帳戶已啟用 TOTP 雙因子驗證</p>
            </div>

            <div class="feature-item">
              <mat-icon class="feature-icon">devices</mat-icon>
              <h3>裝置管理</h3>
              <p>管理您的驗證裝置和備援恢復碼</p>
            </div>

            <div class="feature-item">
              <mat-icon class="feature-icon">lock</mat-icon>
              <h3>即時驗證</h3>
              <p>每次登入都會要求 OTP 驗證碼</p>
            </div>
          </div>

          <div class="action-buttons mt-8 space-y-3">
            <button mat-raised-button color="primary"
                    (click)="goToDeviceManagement()"
                    class="w-full">
              <mat-icon class="mr-2">settings</mat-icon>
              管理我的裝置
            </button>

            <button mat-stroked-button
                    (click)="testOtpVerification()"
                    class="w-full">
              <mat-icon class="mr-2">verified</mat-icon>
              測試 OTP 驗證
            </button>

            <button mat-stroked-button
                    (click)="goToBindDevice()"
                    class="w-full">
              <mat-icon class="mr-2">add_circle</mat-icon>
              綁定新裝置
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .home-card {
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }

    .success-message {
      padding: 32px 16px;
      background: linear-gradient(135deg, #ecfdf5 0%, #f0fdf4 100%);
      border-radius: 12px;
      border: 1px solid #bbf7d0;
    }

    .success-icon {
      font-size: 4rem;
      height: 4rem;
      width: 4rem;
      color: #10b981;
      margin-bottom: 16px;
    }

    .features-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 24px;
      margin: 32px 0;
    }

    .feature-item {
      padding: 24px 16px;
      background-color: #f8fafc;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      text-align: center;
    }

    .feature-icon {
      font-size: 2.5rem;
      height: 2.5rem;
      width: 2.5rem;
      color: #3b82f6;
      margin-bottom: 12px;
    }

    .feature-item h3 {
      font-size: 1.125rem;
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 8px;
    }

    .feature-item p {
      font-size: 0.875rem;
      color: #64748b;
      line-height: 1.4;
    }

    .action-buttons button {
      height: 48px;
      font-size: 1rem;
    }

    mat-card-title {
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
      margin-bottom: 8px;
    }

    mat-card-subtitle {
      font-size: 1rem;
      color: #64748b;
    }
  `]
})
export class HomeComponent {
  constructor(private router: Router) {}

  /**
   * 前往裝置管理頁面
   */
  goToDeviceManagement(): void {
    // 實際應用中應該從認證服務取得使用者資訊
    const accountId = 'demo@example.com';
    this.router.navigate(['/devices/manage'], {
      queryParams: { accountId }
    });
  }

  /**
   * 測試 OTP 驗證
   */
  testOtpVerification(): void {
    const accountId = 'demo@example.com';
    this.router.navigate(['/auth/otp-verify'], {
      queryParams: { accountId }
    });
  }

  /**
   * 前往綁定新裝置頁面
   */
  goToBindDevice(): void {
    this.router.navigate(['/auth/bind-device']);
  }
}
