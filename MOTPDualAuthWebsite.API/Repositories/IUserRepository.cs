using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Repositories
{
    /// <summary>
    /// User Repository 介面
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 根據 ID 取得使用者
        /// </summary>
        Task<User?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根據 Email 取得使用者
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// 建立使用者
        /// </summary>
        Task<User> CreateAsync(User user);

        /// <summary>
        /// 更新使用者
        /// </summary>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// 刪除使用者
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 檢查 Email 是否已存在
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>
        /// 增加登入失敗次數
        /// </summary>
        Task IncrementFailedLoginAttemptsAsync(Guid userId);

        /// <summary>
        /// 重置登入失敗次數
        /// </summary>
        Task ResetFailedLoginAttemptsAsync(Guid userId);

        /// <summary>
        /// 鎖定使用者
        /// </summary>
        Task LockUserAsync(Guid userId, DateTime lockedUntil);

        /// <summary>
        /// 解鎖使用者
        /// </summary>
        Task UnlockUserAsync(Guid userId);
    }
} 