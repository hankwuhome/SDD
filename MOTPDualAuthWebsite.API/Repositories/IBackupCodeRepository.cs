using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Repositories
{
    /// <summary>
    /// Backup Code Repository 介面
    /// </summary>
    public interface IBackupCodeRepository
    {
        /// <summary>
        /// 根據 ID 取得備用驗證碼
        /// </summary>
        Task<BackupCode?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根據使用者 ID 和驗證碼取得記錄
        /// </summary>
        Task<BackupCode?> GetByUserIdAndCodeAsync(Guid userId, string code);

        /// <summary>
        /// 取得使用者的所有備用驗證碼
        /// </summary>
        Task<IEnumerable<BackupCode>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 建立備用驗證碼
        /// </summary>
        Task<BackupCode> CreateAsync(BackupCode backupCode);

        /// <summary>
        /// 更新備用驗證碼
        /// </summary>
        Task<BackupCode> UpdateAsync(BackupCode backupCode);

        /// <summary>
        /// 刪除備用驗證碼
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 刪除使用者的所有備用驗證碼
        /// </summary>
        Task DeleteByUserIdAsync(Guid userId);

        /// <summary>
        /// 標記備用驗證碼為已使用
        /// </summary>
        Task MarkAsUsedAsync(Guid id);
    }
} 