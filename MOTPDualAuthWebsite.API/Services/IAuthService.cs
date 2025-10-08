using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Services
{
    /// <summary>
    /// 認證服務介面
    /// 提供使用者登入、TOTP 驗證、JWT Token 管理等功能
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 使用帳號密碼進行認證
        /// </summary>
        /// <param name="email">電子郵件</param>
        /// <param name="password">密碼</param>
        /// <param name="ipAddress">客戶端 IP 地址</param>
        /// <param name="userAgent">使用者代理字串</param>
        /// <returns>認證結果</returns>
        Task<AuthResult> AuthenticateAsync(string email, string password, string ipAddress, string userAgent);

        /// <summary>
        /// 驗證 TOTP 驗證碼
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="code">TOTP 驗證碼或備用驗證碼</param>
        /// <param name="isBackupCode">是否為備用驗證碼</param>
        /// <param name="ipAddress">客戶端 IP 地址</param>
        /// <param name="userAgent">使用者代理字串</param>
        /// <returns>驗證是否成功</returns>
        Task<bool> VerifyTOTPAsync(Guid userId, string code, bool isBackupCode, string ipAddress, string userAgent);

        /// <summary>
        /// 產生 JWT Token
        /// </summary>
        /// <param name="user">使用者物件</param>
        /// <param name="rememberMe">是否記住登入狀態</param>
        /// <param name="ipAddress">客戶端 IP 地址</param>
        /// <param name="userAgent">使用者代理字串</param>
        /// <returns>JWT Token</returns>
        Task<string> GenerateJWTAsync(User user, bool rememberMe, string ipAddress, string userAgent);

        /// <summary>
        /// 驗證 JWT Token
        /// </summary>
        /// <param name="token">JWT Token</param>
        /// <returns>使用者資訊，如果 Token 無效則返回 null</returns>
        Task<User?> ValidateTokenAsync(string token);

        /// <summary>
        /// 撤銷使用者會話
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="tokenId">Token ID (可選，如果提供則只撤銷指定會話)</param>
        /// <param name="logoutAllDevices">是否登出所有裝置</param>
        /// <returns>撤銷是否成功</returns>
        Task<bool> RevokeSessionAsync(Guid userId, string? tokenId = null, bool logoutAllDevices = false);

        /// <summary>
        /// 取得使用者基本資訊
        /// </summary>
        /// <param name="user">使用者物件</param>
        /// <returns>使用者基本資訊</returns>
        Task<UserInfo> GetUserInfoAsync(User user);

        /// <summary>
        /// 記錄登入失敗嘗試
        /// </summary>
        /// <param name="email">電子郵件</param>
        /// <param name="ipAddress">客戶端 IP 地址</param>
        /// <param name="userAgent">使用者代理字串</param>
        /// <returns>剩餘嘗試次數</returns>
        Task<int> RecordFailedLoginAsync(string email, string ipAddress, string userAgent);

        /// <summary>
        /// 檢查帳號是否被鎖定
        /// </summary>
        /// <param name="email">電子郵件</param>
        /// <returns>鎖定資訊 (是否鎖定, 鎖定到期時間)</returns>
        Task<(bool IsLocked, DateTime? LockedUntil)> CheckAccountLockAsync(string email);

        /// <summary>
        /// 重設帳號鎖定狀態
        /// </summary>
        /// <param name="email">電子郵件</param>
        /// <returns>重設是否成功</returns>
        Task<bool> ResetAccountLockAsync(string email);

        /// <summary>
        /// 清理過期會話
        /// </summary>
        /// <returns>清理的會話數量</returns>
        Task<int> CleanupExpiredSessionsAsync();

        /// <summary>
        /// 取得使用者的活躍會話列表
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <returns>活躍會話列表</returns>
        Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync(Guid userId);

        /// <summary>
        /// 更新使用者最後登入時間
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <returns>更新是否成功</returns>
        Task<bool> UpdateLastLoginAsync(Guid userId);
    }
} 