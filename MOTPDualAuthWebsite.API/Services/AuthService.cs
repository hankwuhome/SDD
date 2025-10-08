using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MOTPDualAuthWebsite.API.Data;
using MOTPDualAuthWebsite.API.Models;
using MOTPDualAuthWebsite.API.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MOTPDualAuthWebsite.API.Services
{
    /// <summary>
    /// 認證服務實作
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOTPService _otpService;
        private readonly IEncryptionService _encryptionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        // 帳號鎖定設定
        private const int MaxFailedAttempts = 5;
        private const int LockoutDurationMinutes = 15;

        public AuthService(
            IUserRepository userRepository,
            IOTPService otpService,
            IEncryptionService encryptionService,
            ApplicationDbContext context,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _encryptionService = encryptionService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 使用帳號密碼進行認證
        /// </summary>
        public async Task<AuthResult> AuthenticateAsync(string email, string password, string ipAddress, string userAgent)
        {
            try
            {
                _logger.LogInformation("開始認證使用者 {Email} 從 IP {IpAddress}", email, ipAddress);

                // 檢查帳號是否被鎖定
                var (isLocked, lockedUntil) = await CheckAccountLockAsync(email);
                if (isLocked)
                {
                    _logger.LogWarning("帳號 {Email} 目前被鎖定至 {LockedUntil}", email, lockedUntil);
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"帳號已被鎖定，請於 {lockedUntil:yyyy-MM-dd HH:mm} 後再試",
                        LockedUntil = lockedUntil
                    };
                }

                // 取得使用者
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("使用者 {Email} 不存在", email);
                    await RecordFailedLoginAsync(email, ipAddress, userAgent);
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "帳號或密碼錯誤"
                    };
                }

                // 驗證密碼
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning("使用者 {Email} 密碼驗證失敗", email);
                    var remainingAttempts = await RecordFailedLoginAsync(email, ipAddress, userAgent);
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "帳號或密碼錯誤",
                        RemainingAttempts = remainingAttempts
                    };
                }

                // 檢查是否需要 TOTP 驗證
                var hasActiveOTP = await _context.OTPCodes
                    .AnyAsync(o => o.UserId == user.Id && o.IsActive);

                if (hasActiveOTP)
                {
                    _logger.LogInformation("使用者 {Email} 需要進行 TOTP 驗證", email);
                    // 重設失敗嘗試次數（帳號密碼正確）
                    user.FailedLoginAttempts = 0;
                    user.LastFailedLoginAt = null;
                    await _userRepository.UpdateAsync(user);

                    return new AuthResult
                    {
                        IsSuccess = true,
                        User = user,
                        RequiresTOTP = true
                    };
                }

                // 完整認證成功（無需 TOTP）
                _logger.LogInformation("使用者 {Email} 認證成功（無需 TOTP）", email);
                await UpdateLastLoginAsync(user.Id);
                
                // 重設失敗嘗試次數
                user.FailedLoginAttempts = 0;
                user.LastFailedLoginAt = null;
                await _userRepository.UpdateAsync(user);

                return new AuthResult
                {
                    IsSuccess = true,
                    User = user,
                    RequiresTOTP = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "認證使用者 {Email} 時發生錯誤", email);
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "認證過程中發生錯誤，請稍後再試"
                };
            }
        }

        /// <summary>
        /// 驗證 TOTP 驗證碼
        /// </summary>
        public async Task<bool> VerifyTOTPAsync(Guid userId, string code, bool isBackupCode, string ipAddress, string userAgent)
        {
            try
            {
                _logger.LogInformation("開始驗證使用者 {UserId} 的 TOTP 驗證碼", userId);

                bool isValid;
                if (isBackupCode)
                {
                    // 驗證備用驗證碼
                    var backupCodes = await _context.BackupCodes
                        .Where(bc => bc.UserId == userId && !bc.IsUsed)
                        .ToListAsync();

                    var matchingCode = backupCodes.FirstOrDefault(bc => 
                        bc.Code == code);

                    if (matchingCode != null)
                    {
                        // 標記備用驗證碼為已使用
                        matchingCode.IsUsed = true;
                        matchingCode.UsedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("使用者 {UserId} 備用驗證碼驗證成功", userId);
                        isValid = true;
                    }
                    else
                    {
                        _logger.LogWarning("使用者 {UserId} 備用驗證碼無效", userId);
                        isValid = false;
                    }
                }
                else
                {
                    // 驗證 TOTP 驗證碼
                    // 取得使用者的活躍 OTP 裝置
                    var otpDevice = await _context.OTPCodes
                        .FirstOrDefaultAsync(o => o.UserId == userId && o.IsActive);

                    if (otpDevice != null)
                    {
                        // 解密密鑰
                        var decryptedSecret = await _encryptionService.DecryptAsync(otpDevice.SecretKey);
                        isValid = await _otpService.ValidateTOTPCodeAsync(decryptedSecret, code);
                    }
                    else
                    {
                        _logger.LogWarning("使用者 {UserId} 沒有活躍的 TOTP 裝置", userId);
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    await UpdateLastLoginAsync(userId);
                    _logger.LogInformation("使用者 {UserId} TOTP 驗證成功", userId);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證使用者 {UserId} TOTP 時發生錯誤", userId);
                return false;
            }
        }

        /// <summary>
        /// 產生 JWT Token
        /// </summary>
        public async Task<string> GenerateJWTAsync(User user, bool rememberMe, string ipAddress, string userAgent)
        {
            try
            {
                _logger.LogInformation("為使用者 {UserId} 產生 JWT Token", user.Id);

                var jwtKey = _configuration["Jwt:Key"] ?? "YourSecretJWTKeyHere12345678901234567890123456789012";
                var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MOTPWebsite";
                var jwtAudience = _configuration["Jwt:Audience"] ?? "MOTPUsers";
                
                // 設定過期時間
                var expiration = rememberMe 
                    ? DateTime.UtcNow.AddDays(30) 
                    : DateTime.UtcNow.AddHours(8);

                var tokenId = Guid.NewGuid().ToString();

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, tokenId),
                    new Claim(JwtRegisteredClaimNames.Iat, 
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                        ClaimValueTypes.Integer64),
                    new Claim("ip", ipAddress),
                    new Claim("remember", rememberMe.ToString().ToLower())
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: expiration,
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // 記錄會話資訊
                var session = new SessionInfo
                {
                    UserId = user.Id,
                    JwtTokenId = tokenId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    LoginAt = DateTime.UtcNow,
                    ExpiresAt = expiration,
                    IsActive = true
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("為使用者 {UserId} 成功產生 JWT Token，過期時間：{Expiration}", 
                    user.Id, expiration);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "為使用者 {UserId} 產生 JWT Token 時發生錯誤", user.Id);
                throw;
            }
        }

        /// <summary>
        /// 驗證 JWT Token
        /// </summary>
        public async Task<User?> ValidateTokenAsync(string token)
        {
            try
            {
                var jwtKey = _configuration["Jwt:Key"] ?? "YourSecretJWTKeyHere12345678901234567890123456789012";
                var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MOTPWebsite";
                var jwtAudience = _configuration["Jwt:Audience"] ?? "MOTPUsers";

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tokenIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (userIdClaim == null || tokenIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return null;
                }

                // 檢查會話是否仍然有效
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.JwtTokenId == tokenIdClaim && s.IsActive);

                if (session == null)
                {
                    _logger.LogWarning("Token {TokenId} 對應的會話不存在或已失效", tokenIdClaim);
                    return null;
                }

                // 取得使用者資訊
                var user = await _userRepository.GetByIdAsync(userId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證 JWT Token 時發生錯誤");
                return null;
            }
        }

        /// <summary>
        /// 撤銷使用者會話
        /// </summary>
        public async Task<bool> RevokeSessionAsync(Guid userId, string? tokenId = null, bool logoutAllDevices = false)
        {
            try
            {
                _logger.LogInformation("撤銷使用者 {UserId} 的會話，TokenId: {TokenId}, 全部登出: {LogoutAll}", 
                    userId, tokenId, logoutAllDevices);

                var query = _context.Sessions.Where(s => s.UserId == userId && s.IsActive);

                if (!logoutAllDevices && !string.IsNullOrEmpty(tokenId))
                {
                    query = query.Where(s => s.JwtTokenId == tokenId);
                }

                var sessions = await query.ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.LogoutAt = DateTime.UtcNow;
                }

                var affectedRows = await _context.SaveChangesAsync();
                
                _logger.LogInformation("成功撤銷 {Count} 個會話", sessions.Count);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤銷使用者 {UserId} 會話時發生錯誤", userId);
                return false;
            }
        }

        /// <summary>
        /// 取得使用者基本資訊
        /// </summary>
        public async Task<UserInfo> GetUserInfoAsync(User user)
        {
            try
            {
                var activeDevicesCount = await _context.OTPCodes
                    .CountAsync(o => o.UserId == user.Id && o.IsActive);

                var lastSession = await _context.Sessions
                    .Where(s => s.UserId == user.Id)
                    .OrderByDescending(s => s.LoginAt)
                    .FirstOrDefaultAsync();

                return new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = lastSession?.LoginAt ?? user.CreatedAt,
                    HasTOTPEnabled = activeDevicesCount > 0,
                    ActiveDevicesCount = activeDevicesCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {UserId} 基本資訊時發生錯誤", user.Id);
                throw;
            }
        }

        /// <summary>
        /// 記錄登入失敗嘗試
        /// </summary>
        public async Task<int> RecordFailedLoginAsync(string email, string ipAddress, string userAgent)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    user.LastFailedLoginAt = DateTime.UtcNow;

                    // 檢查是否需要鎖定帳號
                    if (user.FailedLoginAttempts >= MaxFailedAttempts)
                    {
                        user.IsLocked = true;
                        user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                        _logger.LogWarning("使用者 {Email} 因連續失敗 {Attempts} 次而被鎖定至 {LockedUntil}", 
                            email, user.FailedLoginAttempts, user.LockedUntil);
                    }

                    await _userRepository.UpdateAsync(user);
                    
                    return MaxFailedAttempts - user.FailedLoginAttempts;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄使用者 {Email} 登入失敗時發生錯誤", email);
                return 0;
            }
        }

        /// <summary>
        /// 檢查帳號是否被鎖定
        /// </summary>
        public async Task<(bool IsLocked, DateTime? LockedUntil)> CheckAccountLockAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    return (false, null);
                }

                if (user.IsLocked && user.LockedUntil.HasValue)
                {
                    if (user.LockedUntil.Value > DateTime.UtcNow)
                    {
                        return (true, user.LockedUntil.Value);
                    }
                    else
                    {
                        // 鎖定時間已過，自動解鎖
                        await ResetAccountLockAsync(email);
                        return (false, null);
                    }
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查帳號 {Email} 鎖定狀態時發生錯誤", email);
                return (false, null);
            }
        }

        /// <summary>
        /// 重設帳號鎖定狀態
        /// </summary>
        public async Task<bool> ResetAccountLockAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user != null)
                {
                    user.IsLocked = false;
                    user.LockedUntil = null;
                    user.FailedLoginAttempts = 0;
                    user.LastFailedLoginAt = null;

                    await _userRepository.UpdateAsync(user);
                    _logger.LogInformation("已重設使用者 {Email} 的帳號鎖定狀態", email);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重設使用者 {Email} 帳號鎖定狀態時發生錯誤", email);
                return false;
            }
        }

        /// <summary>
        /// 清理過期會話
        /// </summary>
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            try
            {
                var expiredSessions = await _context.Sessions
                    .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var session in expiredSessions)
                {
                    session.IsActive = false;
                    session.LogoutAt = DateTime.UtcNow;
                }

                var cleanedCount = await _context.SaveChangesAsync();
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("清理了 {Count} 個過期會話", cleanedCount);
                }

                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理過期會話時發生錯誤");
                return 0;
            }
        }

        /// <summary>
        /// 取得使用者的活躍會話列表
        /// </summary>
        public async Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync(Guid userId)
        {
            try
            {
                return await _context.Sessions
                    .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(s => s.LoginAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {UserId} 活躍會話時發生錯誤", userId);
                return new List<SessionInfo>();
            }
        }

        /// <summary>
        /// 更新使用者最後登入時間
        /// </summary>
        public async Task<bool> UpdateLastLoginAsync(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新使用者 {UserId} 最後登入時間時發生錯誤", userId);
                return false;
            }
        }
    }
} 