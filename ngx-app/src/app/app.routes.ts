import { Routes } from '@angular/router';

export const routes: Routes = [
  // 預設路由重導向到綁定裝置頁面
  {
    path: '',
    redirectTo: '/auth/bind-device',
    pathMatch: 'full'
  },

  // 認證相關路由
  {
    path: 'auth',
    children: [
      {
        path: 'bind-device',
        loadComponent: () => import('./features/auth/pages/bind-device.component').then(c => c.BindDeviceComponent),
        title: '綁定 TOTP 裝置'
      },
      {
        path: 'verify-setup',
        loadComponent: () => import('./features/auth/pages/verify-setup.component').then(c => c.VerifySetupComponent),
        title: '驗證設定'
      },
      {
        path: 'otp-verify',
        loadComponent: () => import('./features/auth/pages/otp-verify.component').then(c => c.OtpVerifyComponent),
        title: 'OTP 驗證'
      }
    ]
  },

  // 裝置管理路由
  {
    path: 'devices',
    children: [
      {
        path: 'manage',
        loadComponent: () => import('./features/devices/pages/manage-devices.component').then(c => c.ManageDevicesComponent),
        title: '裝置管理'
      }
    ]
  },

  // 首頁（驗證成功後的頁面）
  {
    path: 'home',
    loadComponent: () => import('./pages/home.component').then(c => c.HomeComponent),
    title: '首頁'
  },

  // 404 頁面
  {
    path: '**',
    redirectTo: '/auth/bind-device'
  }
];
