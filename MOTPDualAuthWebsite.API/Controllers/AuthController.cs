using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MOTPDualAuthWebsite.API.Models;
using MOTPDualAuthWebsite.API.Services;
using System.Security.Claims;

namespace MOTPDualAuthWebsite.API.Controllers
{
    /// <summary>
    /// 認證相關 API 控制器
    /// 提供登入、TOTP 驗證、登出、使用者資訊等功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        /// <param name="request">登入請求</param>
        /// <returns>登入結果</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(typeof(LoginResponse), 400)]
        [ProducesResponseType(typeof(LoginResponse), 401)]
        [ProducesResponseType(typeof(LoginResponse), 423)] // 帳號鎖定
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("收到登入請求，帳號: {Email}", request.Email);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("登入請求模型驗證失敗: {Errors}", errors);
                    
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = errors
                    });
                }

                // 取得客戶端資訊
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // 執行認證
                var authResult = await _authService.AuthenticateAsync(
                    request.Email, 
                    request.Password, 
                    ipAddress, 
                    userAgent);

                if (!authResult.IsSuccess)
                {
                    _logger.LogWarning("使用者 {Email} 登入失敗: {Error}", request.Email, authResult.ErrorMessage);

                    var statusCode = authResult.LockedUntil.HasValue ? 423 : 401;
                    
                    return StatusCode(statusCode, new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = authResult.ErrorMessage,
                        RemainingAttempts = authResult.RemainingAttempts,
                        LockedUntil = authResult.LockedUntil
                    });
                }

                // 認證成功
                if (authResult.RequiresTOTP)
                {
                    // 需要 TOTP 驗證
                    _logger.LogInformation("使用者 {Email} 帳號密碼驗證成功，需要 TOTP 驗證", request.Email);
                    
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        RequiresTOTP = true,
                        UserId = authResult.User!.Id
                    });
                }
                else
                {
                    // 完整認證成功（無需 TOTP）
                    var token = await _authService.GenerateJWTAsync(
                        authResult.User!, 
                        request.RememberMe, 
                        ipAddress, 
                        userAgent);

                    var userInfo = await _authService.GetUserInfoAsync(authResult.User!);
                    
                    _logger.LogInformation("使用者 {Email} 登入成功", request.Email);
                    
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        RequiresTOTP = false,
                        Token = token,
                        User = userInfo
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入過程中發生錯誤");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "系統內部錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// TOTP 驗證
        /// </summary>
        /// <param name="request">TOTP 驗證請求</param>
        /// <returns>驗證結果</returns>
        [HttpPost("verify-totp")]
        [ProducesResponseType(typeof(TOTPVerifyResponse), 200)]
        [ProducesResponseType(typeof(TOTPVerifyResponse), 400)]
        [ProducesResponseType(typeof(TOTPVerifyResponse), 401)]
        public async Task<ActionResult<TOTPVerifyResponse>> VerifyTOTP([FromBody] TOTPVerifyRequest request)
        {
            try
            {
                _logger.LogInformation("收到 TOTP 驗證請求，使用者 ID: {UserId}", request.UserId);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("TOTP 驗證請求模型驗證失敗: {Errors}", errors);
                    
                    return BadRequest(new TOTPVerifyResponse
                    {
                        Success = false,
                        ErrorMessage = errors
                    });
                }

                // 取得客戶端資訊
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // 驗證 TOTP
                var isValid = await _authService.VerifyTOTPAsync(
                    request.UserId, 
                    request.Code, 
                    request.IsBackupCode, 
                    ipAddress, 
                    userAgent);

                if (!isValid)
                {
                    _logger.LogWarning("使用者 {UserId} TOTP 驗證失敗", request.UserId);
                    
                    return Unauthorized(new TOTPVerifyResponse
                    {
                        Success = false,
                        ErrorMessage = request.IsBackupCode ? "備用驗證碼無效或已使用" : "TOTP 驗證碼錯誤或已過期",
                        RemainingAttempts = 2 // 可以從配置中取得
                    });
                }

                // TOTP 驗證成功，產生 JWT Token
                var user = await _authService.ValidateTokenAsync(""); // 需要先取得使用者
                // 暫時直接從 UserRepository 取得使用者
                var userRepository = HttpContext.RequestServices.GetRequiredService<Repositories.IUserRepository>();
                user = await userRepository.GetByIdAsync(request.UserId);

                if (user == null)
                {
                    _logger.LogError("TOTP 驗證成功但找不到使用者 {UserId}", request.UserId);
                    return StatusCode(500, new TOTPVerifyResponse
                    {
                        Success = false,
                        ErrorMessage = "系統內部錯誤"
                    });
                }

                var token = await _authService.GenerateJWTAsync(
                    user, 
                    false, // TOTP 驗證時不記住登入狀態
                    ipAddress, 
                    userAgent);

                var userInfo = await _authService.GetUserInfoAsync(user);
                
                _logger.LogInformation("使用者 {UserId} TOTP 驗證成功", request.UserId);
                
                return Ok(new TOTPVerifyResponse
                {
                    Success = true,
                    Token = token,
                    User = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOTP 驗證過程中發生錯誤");
                return StatusCode(500, new TOTPVerifyResponse
                {
                    Success = false,
                    ErrorMessage = "系統內部錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 使用者登出
        /// </summary>
        /// <param name="request">登出請求</param>
        /// <returns>登出結果</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(LogoutResponse), 200)]
        [ProducesResponseType(typeof(LogoutResponse), 401)]
        public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest? request = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new LogoutResponse
                    {
                        Success = false,
                        Message = "使用者未認證"
                    });
                }

                var tokenId = GetCurrentTokenId();
                var logoutAllDevices = request?.LogoutAllDevices ?? false;

                _logger.LogInformation("使用者 {UserId} 開始登出，全部裝置: {LogoutAll}", userId, logoutAllDevices);

                var success = await _authService.RevokeSessionAsync(
                    userId.Value, 
                    tokenId, 
                    logoutAllDevices);

                if (success)
                {
                    var message = logoutAllDevices ? "已登出所有裝置" : "登出成功";
                    _logger.LogInformation("使用者 {UserId} 登出成功", userId);
                    
                    return Ok(new LogoutResponse
                    {
                        Success = true,
                        Message = message
                    });
                }
                else
                {
                    return StatusCode(500, new LogoutResponse
                    {
                        Success = false,
                        Message = "登出過程中發生錯誤"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出過程中發生錯誤");
                return StatusCode(500, new LogoutResponse
                {
                    Success = false,
                    Message = "系統內部錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 取得使用者資訊
        /// </summary>
        /// <returns>使用者資訊</returns>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserProfileResponse>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                var userRepository = HttpContext.RequestServices.GetRequiredService<Repositories.IUserRepository>();
                var user = await userRepository.GetByIdAsync(userId.Value);

                if (user == null)
                {
                    _logger.LogWarning("找不到使用者 {UserId}", userId);
                    return Unauthorized();
                }

                var userInfo = await _authService.GetUserInfoAsync(user);
                var activeSessions = await _authService.GetActiveSessionsAsync(userId.Value);

                _logger.LogInformation("取得使用者 {UserId} 資訊", userId);

                return Ok(new UserProfileResponse
                {
                    User = userInfo,
                    ActiveSessions = activeSessions.Select(s => new SessionSummary
                    {
                        Id = s.Id,
                        IpAddress = s.IpAddress,
                        UserAgent = s.UserAgent,
                        LoginAt = s.LoginAt,
                        ExpiresAt = s.ExpiresAt,
                        IsCurrent = s.JwtTokenId == GetCurrentTokenId()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者資訊時發生錯誤");
                return StatusCode(500, new { message = "系統內部錯誤，請稍後再試" });
            }
        }

        /// <summary>
        /// 撤銷指定會話
        /// </summary>
        /// <param name="sessionId">會話 ID</param>
        /// <returns>撤銷結果</returns>
        [HttpDelete("sessions/{sessionId}")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> RevokeSession(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                // 這裡需要實作根據 sessionId 撤銷特定會話的邏輯
                // 暫時使用現有的 RevokeSessionAsync 方法
                var success = await _authService.RevokeSessionAsync(userId.Value, sessionId.ToString(), false);

                if (success)
                {
                    _logger.LogInformation("使用者 {UserId} 成功撤銷會話 {SessionId}", userId, sessionId);
                    return Ok(new { message = "會話已撤銷" });
                }
                else
                {
                    return NotFound(new { message = "找不到指定的會話" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤銷會話時發生錯誤");
                return StatusCode(500, new { message = "系統內部錯誤，請稍後再試" });
            }
        }

        #region Helper Methods

        /// <summary>
        /// 取得客戶端 IP 地址
        /// </summary>
        private string GetClientIpAddress()
        {
            var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ipAddress ?? "Unknown";
        }

        /// <summary>
        /// 取得當前使用者 ID
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// 取得當前 Token ID
        /// </summary>
        private string? GetCurrentTokenId()
        {
            return User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        }

        #endregion

        #region Response Models

        /// <summary>
        /// 使用者資訊回應模型
        /// </summary>
        public class UserProfileResponse
        {
            public UserInfo User { get; set; } = null!;
            public List<SessionSummary> ActiveSessions { get; set; } = new();
        }

        /// <summary>
        /// 會話摘要模型
        /// </summary>
        public class SessionSummary
        {
            public Guid Id { get; set; }
            public string IpAddress { get; set; } = string.Empty;
            public string UserAgent { get; set; } = string.Empty;
            public DateTime LoginAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsCurrent { get; set; }
        }

        #endregion
    }
} 