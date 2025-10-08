using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Repositories
{
    /// <summary>
    /// OTP Repository 介面
    /// </summary>
    public interface IOTPRepository
    {
        /// <summary>
        /// 根據 ID 取得 OTP 記錄
        /// </summary>
        Task<OTPCode?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根據使用者 ID 和設備名稱取得 OTP 記錄
        /// </summary>
        Task<OTPCode?> GetByUserIdAndDeviceNameAsync(Guid userId, string deviceName);

        /// <summary>
        /// 取得使用者的所有 OTP 記錄
        /// </summary>
        Task<IEnumerable<OTPCode>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 建立 OTP 記錄
        /// </summary>
        Task<OTPCode> CreateAsync(OTPCode otpCode);

        /// <summary>
        /// 更新 OTP 記錄
        /// </summary>
        Task<OTPCode> UpdateAsync(OTPCode otpCode);

        /// <summary>
        /// 刪除 OTP 記錄
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 檢查使用者是否已有指定名稱的設備
        /// </summary>
        Task<bool> ExistsByUserIdAndDeviceNameAsync(Guid userId, string deviceName);

        /// <summary>
        /// 更新最後使用時間
        /// </summary>
        Task UpdateLastUsedAtAsync(Guid id);
    }
} 