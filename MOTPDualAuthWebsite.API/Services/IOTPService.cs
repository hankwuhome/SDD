using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Services
{
    /// <summary>
    /// OTP 服務介面
    /// </summary>
    public interface IOTPService
    {
        /// <summary>
        /// 產生 QR Code 用於設備綁定
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="deviceName">設備名稱</param>
        /// <returns>QR Code 資料 (Base64)</returns>
        Task<string> GenerateQRCodeAsync(Guid userId, string deviceName);

        /// <summary>
        /// 驗證設備設置
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="deviceName">設備名稱</param>
        /// <param name="otpCode">OTP 驗證碼</param>
        /// <returns>驗證結果</returns>
        Task<bool> VerifySetupAsync(Guid userId, string deviceName, string otpCode);

        /// <summary>
        /// 驗證 OTP 驗證碼
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="otpCode">OTP 驗證碼</param>
        /// <returns>驗證結果</returns>
        Task<bool> VerifyCodeAsync(Guid userId, string otpCode);

        /// <summary>
        /// 取得使用者的所有設備
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <returns>設備清單</returns>
        Task<IEnumerable<OTPCode>> GetUserDevicesAsync(Guid userId);

        /// <summary>
        /// 刪除設備
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="deviceId">設備 ID</param>
        Task DeleteDeviceAsync(Guid userId, Guid deviceId);

        /// <summary>
        /// 產生備用驗證碼
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="count">產生數量</param>
        /// <returns>備用驗證碼清單</returns>
        Task<IEnumerable<string>> GenerateBackupCodesAsync(Guid userId, int count = 10);

        /// <summary>
        /// 產生 TOTP 驗證碼
        /// </summary>
        /// <param name="secretKey">密鑰</param>
        /// <returns>TOTP 驗證碼</returns>
        Task<string> GenerateTOTPCodeAsync(string secretKey);

        /// <summary>
        /// 驗證 TOTP 驗證碼
        /// </summary>
        /// <param name="secretKey">密鑰</param>
        /// <param name="code">驗證碼</param>
        /// <returns>驗證結果</returns>
        Task<bool> ValidateTOTPCodeAsync(string secretKey, string code, int toleranceMinutes = 5);
    }
} 