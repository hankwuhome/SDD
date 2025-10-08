import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';

/**
 * 備援恢復碼對話框
 */
@Component({
  selector: 'app-recovery-codes-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  template: `
    <div class="recovery-codes-dialog">
      <h2 mat-dialog-title class="flex items-center">
        <mat-icon class="mr-2 text-orange-600">backup</mat-icon>
        備援恢復碼
      </h2>

      <mat-dialog-content class="dialog-content">
        <div class="warning-message mb-4 p-4 bg-orange-50 border border-orange-200 rounded-lg">
          <div class="flex items-start">
            <mat-icon class="mr-2 text-orange-600 mt-1">warning</mat-icon>
            <div class="text-orange-800">
              <h4 class="font-semibold mb-2">重要提醒</h4>
              <ul class="text-sm space-y-1">
                <li>• 請將這些恢復碼保存在安全的地方</li>
                <li>• 每個恢復碼只能使用一次</li>
                <li>• 當您無法使用驗證器應用程式時可以使用</li>
                <li>• 關閉此對話框後將無法再次查看</li>
              </ul>
            </div>
          </div>
        </div>

        <div class="recovery-codes-container">
          <h3 class="text-lg font-semibold mb-3 text-center">您的備援恢復碼</h3>
          
          <div class="codes-grid">
            <div *ngFor="let code of data.recoveryCodes; let i = index" 
                 class="code-item">
              <span class="code-number">{{ i + 1 }}.</span>
              <span class="code-value">{{ code }}</span>
            </div>
          </div>
        </div>

        <div class="actions-info mt-6 p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <div class="flex items-center text-blue-800">
            <mat-icon class="mr-2 text-blue-600">info</mat-icon>
            <span class="text-sm">
              建議您將這些恢復碼複製到安全的密碼管理器或列印保存
            </span>
          </div>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <button mat-stroked-button (click)="copyAllCodes()">
          <mat-icon class="mr-1">content_copy</mat-icon>
          複製全部
        </button>
        
        <button mat-stroked-button (click)="downloadCodes()">
          <mat-icon class="mr-1">download</mat-icon>
          下載
        </button>
        
        <div class="flex-grow"></div>
        
        <button mat-raised-button color="primary" (click)="close()">
          <mat-icon class="mr-1">check</mat-icon>
          我已安全保存
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .recovery-codes-dialog {
      min-width: 480px;
      max-width: 600px;
    }

    .dialog-content {
      padding: 20px 24px;
    }

    .codes-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
      margin: 16px 0;
      padding: 16px;
      background-color: #f8fafc;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }

    .code-item {
      display: flex;
      align-items: center;
      padding: 8px 12px;
      background-color: white;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
      font-family: 'Courier New', monospace;
    }

    .code-number {
      color: #64748b;
      font-size: 0.875rem;
      margin-right: 8px;
      min-width: 20px;
    }

    .code-value {
      font-weight: 600;
      color: #1e293b;
      letter-spacing: 0.5px;
    }

    .dialog-actions {
      padding: 16px 24px;
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .flex-grow {
      flex: 1;
    }

    .warning-message,
    .actions-info {
      border-left: 4px solid;
    }

    .warning-message {
      border-left-color: #f59e0b;
    }

    .actions-info {
      border-left-color: #3b82f6;
    }

    mat-dialog-title {
      margin: 0;
      padding: 20px 24px;
      border-bottom: 1px solid #e2e8f0;
    }

    h2[mat-dialog-title] {
      font-size: 1.25rem;
      font-weight: 600;
      color: #1e293b;
    }
  `]
})
export class RecoveryCodesDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<RecoveryCodesDialogComponent>);
  private readonly snackBar = inject(MatSnackBar);
  
  /** 注入的對話框資料 */
  readonly data: { recoveryCodes: string[] } = inject(MAT_DIALOG_DATA);

  /**
   * 複製所有恢復碼到剪貼簿
   */
  copyAllCodes(): void {
    const codesText = this.data.recoveryCodes
      .map((code, index) => `${index + 1}. ${code}`)
      .join('\n');

    if (navigator.clipboard) {
      navigator.clipboard.writeText(codesText).then(() => {
        this.snackBar.open('恢復碼已複製到剪貼簿', '關閉', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
      }).catch(() => {
        this.fallbackCopy(codesText);
      });
    } else {
      this.fallbackCopy(codesText);
    }
  }

  /**
   * 下載恢復碼為文字檔案
   */
  downloadCodes(): void {
    const codesText = [
      'MOTP 備援恢復碼',
      '===================',
      '',
      '重要提醒：',
      '• 請將這些恢復碼保存在安全的地方',
      '• 每個恢復碼只能使用一次',
      '• 當您無法使用驗證器應用程式時可以使用',
      '',
      '恢復碼：',
      ...this.data.recoveryCodes.map((code, index) => `${index + 1}. ${code}`),
      '',
      `產生時間：${new Date().toLocaleString('zh-TW')}`
    ].join('\n');

    const blob = new Blob([codesText], { type: 'text/plain;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `MOTP-恢復碼-${new Date().toISOString().split('T')[0]}.txt`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);

    this.snackBar.open('恢復碼已下載', '關閉', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  /**
   * 關閉對話框
   */
  close(): void {
    this.dialogRef.close();
  }

  /**
   * 備用複製方法（舊版瀏覽器）
   */
  private fallbackCopy(text: string): void {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
      document.execCommand('copy');
      this.snackBar.open('恢復碼已複製到剪貼簿', '關閉', {
        duration: 3000,
        panelClass: ['success-snackbar']
      });
    } catch (err) {
      this.snackBar.open('無法複製，請手動選取複製', '關閉', {
        duration: 5000,
        panelClass: ['error-snackbar']
      });
    } finally {
      document.body.removeChild(textArea);
    }
  }
} 