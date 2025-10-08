using Microsoft.EntityFrameworkCore;
using MOTPDualAuthWebsite.API.Data;
using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Repositories
{
    /// <summary>
    /// Backup Code Repository 實作
    /// </summary>
    public class BackupCodeRepository : IBackupCodeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BackupCodeRepository> _logger;

        public BackupCodeRepository(ApplicationDbContext context, ILogger<BackupCodeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 根據 ID 取得備用驗證碼
        /// </summary>
        public async Task<BackupCode?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.BackupCodes.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得備用驗證碼 {Id} 時發生錯誤", id);
                throw;
            }
        }

        /// <summary>
        /// 根據使用者 ID 和驗證碼取得記錄
        /// </summary>
        public async Task<BackupCode?> GetByUserIdAndCodeAsync(Guid userId, string code)
        {
            try
            {
                return await _context.BackupCodes
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.Code == code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據使用者 {UserId} 和驗證碼取得備用驗證碼時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 取得使用者的所有備用驗證碼
        /// </summary>
        public async Task<IEnumerable<BackupCode>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                return await _context.BackupCodes
                    .Where(b => b.UserId == userId)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {UserId} 的所有備用驗證碼時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 建立備用驗證碼
        /// </summary>
        public async Task<BackupCode> CreateAsync(BackupCode backupCode)
        {
            try
            {
                _context.BackupCodes.Add(backupCode);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功建立備用驗證碼 {Id}", backupCode.Id);
                return backupCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立備用驗證碼時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 更新備用驗證碼
        /// </summary>
        public async Task<BackupCode> UpdateAsync(BackupCode backupCode)
        {
            try
            {
                _context.BackupCodes.Update(backupCode);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功更新備用驗證碼 {Id}", backupCode.Id);
                return backupCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新備用驗證碼 {Id} 時發生錯誤", backupCode.Id);
                throw;
            }
        }

        /// <summary>
        /// 刪除備用驗證碼
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var backupCode = await GetByIdAsync(id);
                if (backupCode != null)
                {
                    _context.BackupCodes.Remove(backupCode);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("成功刪除備用驗證碼 {Id}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除備用驗證碼 {Id} 時發生錯誤", id);
                throw;
            }
        }

        /// <summary>
        /// 刪除使用者的所有備用驗證碼
        /// </summary>
        public async Task DeleteByUserIdAsync(Guid userId)
        {
            try
            {
                var backupCodes = await _context.BackupCodes
                    .Where(b => b.UserId == userId)
                    .ToListAsync();

                if (backupCodes.Any())
                {
                    _context.BackupCodes.RemoveRange(backupCodes);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("成功刪除使用者 {UserId} 的 {Count} 個備用驗證碼", userId, backupCodes.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除使用者 {UserId} 的所有備用驗證碼時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 標記備用驗證碼為已使用
        /// </summary>
        public async Task MarkAsUsedAsync(Guid id)
        {
            try
            {
                var backupCode = await GetByIdAsync(id);
                if (backupCode != null && !backupCode.IsUsed)
                {
                    backupCode.IsUsed = true;
                    backupCode.UsedAt = DateTime.UtcNow;
                    await UpdateAsync(backupCode);
                    
                    _logger.LogInformation("備用驗證碼 {Id} 已標記為已使用", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "標記備用驗證碼 {Id} 為已使用時發生錯誤", id);
                throw;
            }
        }
    }
} 