using Microsoft.EntityFrameworkCore;
using MOTPDualAuthWebsite.API.Data;
using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Repositories
{
    /// <summary>
    /// User Repository 實作
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 根據 ID 取得使用者
        /// </summary>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.Users.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {Id} 時發生錯誤", id);
                throw;
            }
        }

        /// <summary>
        /// 根據 Email 取得使用者
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據 Email {Email} 取得使用者時發生錯誤", email);
                throw;
            }
        }

        /// <summary>
        /// 建立使用者
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功建立使用者 {UserId} ({Email})", user.Id, user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立使用者 {Email} 時發生錯誤", user.Email);
                throw;
            }
        }

        /// <summary>
        /// 更新使用者
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功更新使用者 {UserId}", user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新使用者 {UserId} 時發生錯誤", user.Id);
                throw;
            }
        }

        /// <summary>
        /// 刪除使用者
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var user = await GetByIdAsync(id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("成功刪除使用者 {UserId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除使用者 {UserId} 時發生錯誤", id);
                throw;
            }
        }

        /// <summary>
        /// 檢查 Email 是否已存在
        /// </summary>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == email.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查 Email {Email} 是否存在時發生錯誤", email);
                throw;
            }
        }

        /// <summary>
        /// 增加登入失敗次數
        /// </summary>
        public async Task IncrementFailedLoginAttemptsAsync(Guid userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    user.LastFailedLoginAt = DateTime.UtcNow;
                    await UpdateAsync(user);
                    
                    _logger.LogInformation("使用者 {UserId} 登入失敗次數增加至 {Count}", userId, user.FailedLoginAttempts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "增加使用者 {UserId} 登入失敗次數時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 重置登入失敗次數
        /// </summary>
        public async Task ResetFailedLoginAttemptsAsync(Guid userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.FailedLoginAttempts = 0;
                    user.LastFailedLoginAt = null;
                    await UpdateAsync(user);
                    
                    _logger.LogInformation("使用者 {UserId} 登入失敗次數已重置", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置使用者 {UserId} 登入失敗次數時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 鎖定使用者
        /// </summary>
        public async Task LockUserAsync(Guid userId, DateTime lockedUntil)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.IsLocked = true;
                    user.LockedUntil = lockedUntil;
                    await UpdateAsync(user);
                    
                    _logger.LogInformation("使用者 {UserId} 已被鎖定至 {LockedUntil}", userId, lockedUntil);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "鎖定使用者 {UserId} 時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 解鎖使用者
        /// </summary>
        public async Task UnlockUserAsync(Guid userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.IsLocked = false;
                    user.LockedUntil = null;
                    user.FailedLoginAttempts = 0;
                    user.LastFailedLoginAt = null;
                    await UpdateAsync(user);
                    
                    _logger.LogInformation("使用者 {UserId} 已解鎖", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解鎖使用者 {UserId} 時發生錯誤", userId);
                throw;
            }
        }
    }
} 