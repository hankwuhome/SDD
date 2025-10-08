using Microsoft.EntityFrameworkCore;
using MOTPDualAuthWebsite.API.Data;
using MOTPDualAuthWebsite.API.Models;

namespace MOTPDualAuthWebsite.API.Repositories
{
    /// <summary>
    /// OTP Repository 實作
    /// </summary>
    public class OTPRepository : IOTPRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OTPRepository> _logger;

        public OTPRepository(ApplicationDbContext context, ILogger<OTPRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 根據 ID 取得 OTP 記錄
        /// </summary>
        public async Task<OTPCode?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.OTPCodes.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得 OTP 記錄 {Id} 時發生錯誤", id);
                throw;
            }
        }

        /// <summary>
        /// 根據使用者 ID 和設備名稱取得 OTP 記錄
        /// </summary>
        public async Task<OTPCode?> GetByUserIdAndDeviceNameAsync(Guid userId, string deviceName)
        {
            try
            {
                return await _context.OTPCodes
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.DeviceName == deviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {UserId} 設備 {DeviceName} 的 OTP 記錄時發生錯誤", userId, deviceName);
                throw;
            }
        }

        /// <summary>
        /// 取得使用者的所有 OTP 記錄
        /// </summary>
        public async Task<IEnumerable<OTPCode>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                return await _context.OTPCodes
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {UserId} 的所有 OTP 記錄時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 建立 OTP 記錄
        /// </summary>
        public async Task<OTPCode> CreateAsync(OTPCode otpCode)
        {
            try
            {
                _context.OTPCodes.Add(otpCode);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功建立 OTP 記錄 {Id}", otpCode.Id);
                return otpCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立 OTP 記錄時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 更新 OTP 記錄
        /// </summary>
        public async Task<OTPCode> UpdateAsync(OTPCode otpCode)
        {
            try
            {
                _context.OTPCodes.Update(otpCode);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功更新 OTP 記錄 {Id}", otpCode.Id);
                return otpCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 OTP 記錄 {Id} 時發生錯誤", otpCode.Id);
                throw;
            }
        }

        /// <summary>
        /// 刪除 OTP 記錄
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var otpCode = await GetByIdAsync(id);
                if (otpCode != null)
                {
                    _context.OTPCodes.Remove(otpCode);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("成功刪除 OTP 記錄 {Id}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除 OTP 記錄 {Id} 時發生錯誤", id);
                throw;
            }
        }

        /// <summary>
        /// 檢查使用者是否已有指定名稱的設備
        /// </summary>
        public async Task<bool> ExistsByUserIdAndDeviceNameAsync(Guid userId, string deviceName)
        {
            try
            {
                return await _context.OTPCodes
                    .AnyAsync(o => o.UserId == userId && o.DeviceName == deviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查使用者 {UserId} 設備 {DeviceName} 是否存在時發生錯誤", userId, deviceName);
                throw;
            }
        }

        /// <summary>
        /// 更新最後使用時間
        /// </summary>
        public async Task UpdateLastUsedAtAsync(Guid id)
        {
            try
            {
                var otpCode = await GetByIdAsync(id);
                if (otpCode != null)
                {
                    otpCode.LastUsedAt = DateTime.UtcNow;
                    await UpdateAsync(otpCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 OTP 記錄 {Id} 最後使用時間時發生錯誤", id);
                throw;
            }
        }
    }
} 