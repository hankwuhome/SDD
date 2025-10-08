import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
    <div class="app-container">
      <!-- 應用程式工具列 -->
      <mat-toolbar color="primary" class="app-toolbar">
        <div class="toolbar-content">
          <div class="logo-section">
            <mat-icon class="logo-icon">security</mat-icon>
            <span class="app-title">MOTP 雙因子驗證</span>
          </div>

          <span class="toolbar-spacer"></span>

          <div class="toolbar-actions">
            <button mat-icon-button class="help-button" aria-label="說明">
              <mat-icon class="help-icon">help_outline</mat-icon>
            </button>
          </div>
        </div>
      </mat-toolbar>

      <!-- 主要內容區域 -->
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <!-- 頁腳 -->
      <footer class="app-footer">
        <p>© 2024 MOTP 雙因子驗證系統 - 保護您的帳戶安全</p>
      </footer>
    </div>
  `,
  styles: [`
    .app-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      height: 64px;
      min-height: 64px;
    }

    .toolbar-content {
      display: flex;
      align-items: center;
      width: 100%;
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 16px;
      height: 100%;
    }

    .logo-section {
      display: flex;
      align-items: center;
      gap: 16px;
      height: 100%;
    }

    .logo-icon {
      font-size: 28px !important;
      height: 28px !important;
      width: 28px !important;
      line-height: 28px !important;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .app-title {
      font-size: 20px;
      font-weight: 500;
      color: white;
      white-space: nowrap;
      line-height: 1.2;
    }

    .toolbar-actions {
      display: flex;
      align-items: center;
      height: 100%;
    }

    .help-button {
      color: white !important;
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .help-icon {
      font-size: 24px !important;
      height: 24px !important;
      width: 24px !important;
      line-height: 24px !important;
      color: white;
    }

    .app-footer {
      background-color: #424242;
      color: white;
      text-align: center;
      padding: 16px;
      margin-top: auto;
    }

    .app-footer p {
      margin: 0;
      font-size: 14px;
    }

    /* 確保 Material Icons 正確載入 */
    mat-icon {
      font-family: 'Material Icons' !important;
      font-weight: normal;
      font-style: normal;
      display: inline-block;
      line-height: 1;
      text-transform: none;
      letter-spacing: normal;
      word-wrap: normal;
      white-space: nowrap;
      direction: ltr;
      -webkit-font-smoothing: antialiased;
      text-rendering: optimizeLegibility;
      -moz-osx-font-smoothing: grayscale;
      font-feature-settings: 'liga';
    }

    /* 響應式設計 */
    @media (max-width: 768px) {
      .app-toolbar {
        height: 56px;
        min-height: 56px;
      }

      .toolbar-content {
        padding: 0 8px;
      }

      .app-title {
        font-size: 18px;
      }

      .logo-icon {
        font-size: 24px !important;
        height: 24px !important;
        width: 24px !important;
        line-height: 24px !important;
      }

      .help-button {
        width: 40px;
        height: 40px;
      }

      .help-icon {
        font-size: 20px !important;
        height: 20px !important;
        width: 20px !important;
        line-height: 20px !important;
      }
    }

    @media (max-width: 480px) {
      .app-title {
        font-size: 16px;
      }

      .logo-section {
        gap: 12px;
      }
    }
  `]
})
export class AppComponent {
  title = 'MOTP 雙因子驗證系統';
}
