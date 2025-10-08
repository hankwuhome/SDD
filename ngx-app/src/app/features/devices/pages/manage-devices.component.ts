import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router } from '@angular/router';

import { OtpService } from '../../../shared/services/otp.service';
import { DeviceInfo, LoadingState } from '../../../shared/types/otp';
import { RecoveryCodesDialogComponent } from '../../../shared/components/recovery-codes-dialog.component';

/**
 * 裝置管理頁面 - 管理已綁定的 TOTP 裝置
 */
@Component({
  selector: 'app-manage-devices',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="container mx-auto px-4 py-8 max-w-4xl">
      <mat-card class="manage-devices-card">
        <mat-card-header>
          <mat-card-title class="flex items-center">
            <mat-icon class="mr-2">devices</mat-icon>
            裝置管理
          </mat-card-title>
          <mat-card-subtitle>
            管理您的 TOTP 驗證裝置和備援恢復碼
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <!-- 使用者資訊 -->
          <div class="user-info mb-6 p-4 bg-gray-50 rounded-lg">
            <p class="text-sm text-gray-600">
              帳號: <strong>{{ accountId() }}</strong>
            </p>
          </div>

          <!-- 載入中狀態 -->
          <div *ngIf="loadingState().isLoading" class="text-center py-8">
            <mat-spinner class="mx-auto"></mat-spinner>
            <p class="mt-4 text-gray-600">載入裝置清單中...</p>
          </div>

          <!-- 裝置清單 -->
          <div *ngIf="!loadingState().isLoading">
            <div class="flex justify-between items-center mb-4">
              <h3 class="text-lg font-semibold">已綁定裝置</h3>
              <div class="action-buttons space-x-2">
                <button mat-raised-button color="primary"
                        (click)="addNewDevice()">
                  <mat-icon class="mr-1">add</mat-icon>
                  新增裝置
                </button>
                <button mat-stroked-button
                        (click)="generateRecoveryCodes()">
                  <mat-icon class="mr-1">backup</mat-icon>
                  產生備援碼
                </button>
              </div>
            </div>

            <!-- 裝置表格 -->
            <div class="devices-table-container">
              <table mat-table [dataSource]="devices()" class="w-full">
                <!-- 裝置名稱欄 -->
                <ng-container matColumnDef="deviceName">
                  <th mat-header-cell *matHeaderCellDef>裝置名稱</th>
                  <td mat-cell *matCellDef="let device">
                    <div class="flex items-center">
                      <mat-icon class="mr-2 text-gray-500">
                        {{ device.isActive ? 'smartphone' : 'smartphone_off' }}
                      </mat-icon>
                      {{ device.deviceName }}
                    </div>
                  </td>
                </ng-container>

                <!-- 狀態欄 -->
                <ng-container matColumnDef="status">
                  <th mat-header-cell *matHeaderCellDef>狀態</th>
                  <td mat-cell *matCellDef="let device">
                    <span class="status-badge"
                          [class.active]="device.isActive"
                          [class.inactive]="!device.isActive">
                      {{ device.isActive ? '已啟用' : '未啟用' }}
                    </span>
                  </td>
                </ng-container>

                <!-- 建立時間欄 -->
                <ng-container matColumnDef="createdAt">
                  <th mat-header-cell *matHeaderCellDef>建立時間</th>
                  <td mat-cell *matCellDef="let device">
                    {{ formatDate(device.createdAt) }}
                  </td>
                </ng-container>

                <!-- 最後使用時間欄 -->
                <ng-container matColumnDef="lastUsedAt">
                  <th mat-header-cell *matHeaderCellDef>最後使用</th>
                  <td mat-cell *matCellDef="let device">
                    {{ device.lastUsedAt ? formatDate(device.lastUsedAt) : '從未使用' }}
                  </td>
                </ng-container>

                <!-- 操作欄 -->
                <ng-container matColumnDef="actions">
                  <th mat-header-cell *matHeaderCellDef>操作</th>
                  <td mat-cell *matCellDef="let device">
                    <button mat-icon-button color="warn"
                            (click)="deleteDevice(device)"
                            [disabled]="deletingDeviceId() === device.id">
                      <mat-spinner diameter="20" *ngIf="deletingDeviceId() === device.id"></mat-spinner>
                      <mat-icon *ngIf="deletingDeviceId() !== device.id">delete</mat-icon>
                    </button>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
              </table>
            </div>

            <!-- 無裝置狀態 -->
            <div *ngIf="devices().length === 0" class="no-devices text-center py-8">
              <mat-icon class="text-gray-400 text-6xl mb-4">device_unknown</mat-icon>
              <h3 class="text-gray-600 mb-2">尚未綁定任何裝置</h3>
              <p class="text-gray-500 mb-4">請先綁定 TOTP 驗證裝置以啟用雙因子驗證</p>
              <button mat-raised-button color="primary"
                      (click)="addNewDevice()">
                <mat-icon class="mr-1">add</mat-icon>
                立即綁定
              </button>
            </div>
          </div>

          <!-- 返回按鈕 -->
          <div class="mt-6 text-center">
            <button mat-stroked-button (click)="goBack()">
              <mat-icon class="mr-1">arrow_back</mat-icon>
              返回驗證頁面
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .manage-devices-card {
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }

    .devices-table-container {
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      overflow: hidden;
    }

    .status-badge {
      padding: 4px 8px;
      border-radius: 12px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .status-badge.active {
      background-color: #dcfce7;
      color: #166534;
    }

    .status-badge.inactive {
      background-color: #fef3c7;
      color: #92400e;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
    }

    .no-devices {
      background-color: #f9fafb;
      border-radius: 8px;
      margin: 16px 0;
    }

    mat-table {
      background: white;
    }

    .mat-mdc-header-cell {
      font-weight: 600;
      color: #374151;
    }
  `]
})
export class ManageDevicesComponent implements OnInit {
  private readonly otpService = inject(OtpService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);

  /** 帳號 ID */
  accountId = signal<string>('');

  /** 裝置清單 */
  devices = signal<DeviceInfo[]>([]);

  /** 載入狀態 */
  loadingState = signal<LoadingState>({
    isLoading: false,
    hasError: false,
    errorMessage: ''
  });

  /** 正在刪除的裝置 ID */
  deletingDeviceId = signal<string>('');

  /** 表格顯示欄位 */
  displayedColumns: string[] = ['deviceName', 'status', 'createdAt', 'lastUsedAt', 'actions'];

  ngOnInit(): void {
    // 從查詢參數取得帳號 ID
    this.route.queryParams.subscribe(params => {
      const accountId = params['accountId'];
      if (accountId) {
        this.accountId.set(accountId);
        this.loadDevices();
      } else {
        // 如果沒有帳號 ID，返回綁定頁面
        this.router.navigate(['/auth/bind-device']);
      }
    });
  }

  /**
   * 載入裝置清單
   */
  loadDevices(): void {
    this.loadingState.set({
      isLoading: true,
      hasError: false,
      errorMessage: ''
    });

    // 暫時使用固定的 userId - 實際應該從認證服務取得
    const userId = '123e4567-e89b-12d3-a456-426614174000';

    this.otpService.getUserDevices(userId).subscribe({
      next: (devices) => {
        this.loadingState.set({
          isLoading: false,
          hasError: false,
          errorMessage: ''
        });
        this.devices.set(devices);
      },
      error: (error) => {
        this.loadingState.set({
          isLoading: false,
          hasError: true,
          errorMessage: error.message
        });
        this.snackBar.open(error.message, '關閉', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }

  /**
   * 新增裝置
   */
  addNewDevice(): void {
    this.router.navigate(['/auth/bind-device']);
  }

  /**
   * 刪除裝置
   */
  deleteDevice(device: DeviceInfo): void {
    if (!confirm(`確定要刪除裝置「${device.deviceName}」嗎？`)) {
      return;
    }

    this.deletingDeviceId.set(device.id);

    // 暫時使用固定的 userId
    const userId = '123e4567-e89b-12d3-a456-426614174000';

    this.otpService.deleteDevice(userId, device.id).subscribe({
      next: () => {
        this.deletingDeviceId.set('');
        this.snackBar.open(`裝置「${device.deviceName}」已刪除`, '關閉', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        // 重新載入裝置清單
        this.loadDevices();
      },
      error: (error) => {
        this.deletingDeviceId.set('');
        this.snackBar.open(error.message, '關閉', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }

  /**
   * 產生備援恢復碼
   */
  generateRecoveryCodes(): void {
    // 暫時使用固定的 userId
    const userId = '123e4567-e89b-12d3-a456-426614174000';

    this.otpService.generateRecoveryCodes(userId).subscribe({
      next: (response) => {
        if (response.success) {
          // 開啟備援碼對話框
          this.dialog.open(RecoveryCodesDialogComponent, {
            data: { recoveryCodes: response.recoveryCodes },
            disableClose: true,
            width: '500px'
          });
        } else {
          this.snackBar.open(response.errorMessage || '產生備援碼失敗', '關閉', {
            duration: 5000,
            panelClass: ['error-snackbar']
          });
        }
      },
      error: (error) => {
        this.snackBar.open(error.message, '關閉', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }

  /**
   * 返回驗證頁面
   */
  goBack(): void {
    this.router.navigate(['/auth/otp-verify'], {
      queryParams: { accountId: this.accountId() }
    });
  }

  /**
   * 格式化日期
   */
  formatDate(dateString: string): string {
    try {
      const date = new Date(dateString);
      return date.toLocaleString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return '無效日期';
    }
  }
}
