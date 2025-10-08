using Microsoft.AspNetCore.Mvc;
using MOTPDualAuthWebsite.API.Models;
using MOTPDualAuthWebsite.API.Services;
using MOTPDualAuthWebsite.API.Repositories;
using System.ComponentModel.DataAnnotations;

namespace MOTPDualAuthWebsite.API.Controllers
{
    /// <summary>
    /// OTP 相關 API 控制器
    /// 提供裝置綁定、OTP 驗證、裝置管理等功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OTPController : ControllerBase
    {
        private readonly IOTPService _otpService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OTPController> _logger;

        public OTPController(IOTPService otpService, IUserRepository userRepository, ILogger<OTPController> logger)
        {
            _otpService = otpService;
            _userRepository = userRepository;
            _logger = logger;
        }

        #region Model 
        /// <summary>
        /// 裝置綁定請求模型
        /// </summary>
        public class BindDeviceRequest
        {
            [Required(ErrorMessage = "帳號為必填欄位")]
            [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
            public string AccountId { get; set; } = string.Empty;

            [Required(ErrorMessage = "密碼為必填欄位")]
            [MinLength(6, ErrorMessage = "密碼至少需要 6 個字元")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "裝置名稱為必填欄位")]
            [StringLength(100, ErrorMessage = "裝置名稱不能超過 100 個字元")]
            public string DeviceName { get; set; } = string.Empty;
        }

        /// <summary>
        /// 裝置綁定回應模型
        /// </summary>
        public class BindDeviceResponse
        {
            public string QRCodeData { get; set; } = string.Empty;
            public string SecretKey { get; set; } = string.Empty;
            public string OtpAuthUri { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// OTP 驗證請求模型
        /// </summary>
        public class VerifyOTPRequest
        {
            [Required(ErrorMessage = "帳號為必填欄位")]
            [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
            public string AccountId { get; set; } = string.Empty;

            [Required(ErrorMessage = "OTP 代碼為必填欄位")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP 代碼必須為 6 位數字")]
            [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP 代碼必須為 6 位數字")]
            public string Code { get; set; } = string.Empty;

            public int LeewayWindows { get; set; } = 1;
            public long? ClientTimeMs { get; set; }
        }

        /// <summary>
        /// OTP 驗證回應模型
        /// </summary>
        public class VerifyOTPResponse
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public int RemainingAttempts { get; set; }
        }

        /// <summary>
        /// 裝置資訊模型
        /// </summary>
        public class DeviceInfo
        {
            public Guid Id { get; set; }
            public string DeviceName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? LastUsedAt { get; set; }
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// 備援恢復碼回應模型
        /// </summary>
        public class RecoveryCodesResponse
        {
            public List<string> RecoveryCodes { get; set; } = new List<string>();
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
        }

        #endregion

        #region 新帳號註冊
        
        /// <summary>
        /// 裝置綁定 - 產生 QR Code 和秘密金鑰
        /// </summary>
        [HttpPost("devices/bind")]
        [ProducesResponseType(typeof(BindDeviceResponse), 200)]
        [ProducesResponseType(typeof(BindDeviceResponse), 400)]
        public async Task<ActionResult<BindDeviceResponse>> BindDevice([FromBody] BindDeviceRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理裝置綁定請求，帳號: {AccountId}, 裝置: {DeviceName}",
                    request.AccountId, request.DeviceName);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("模型驗證失敗: {Errors}", errors);

                    return BadRequest(new BindDeviceResponse
                    {
                        Success = false,
                        ErrorMessage = errors
                    });
                }

                // 檢查使用者是否存在，不存在則建立
                var user = await _userRepository.GetByEmailAsync(request.AccountId);
                if (user == null)
                {
                    // 建立新使用者
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = request.AccountId,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        FailedLoginAttempts = 0,
                        IsLocked = false
                    };

                    await _userRepository.CreateAsync(user);
                    _logger.LogInformation("建立新使用者: {Email}", request.AccountId);
                }

                // 產生 QR Code
                var qrCodeData = await _otpService.GenerateQRCodeAsync(user.Id, request.DeviceName);

                var response = new BindDeviceResponse
                {
                    Success = true,
                    QRCodeData = qrCodeData,
                    SecretKey = "", // 實際應該從服務取得
                    OtpAuthUri = "" // 實際應該從服務取得
                };

                _logger.LogInformation("裝置綁定成功，使用者: {UserId}", user.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "裝置綁定過程中發生錯誤");
                return StatusCode(500, new BindDeviceResponse
                {
                    Success = false,
                    ErrorMessage = "系統內部錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 驗證裝置設定 - 用於初次綁定後的驗證
        /// </summary>
        [HttpPost("devices/verify-setup")]
        [ProducesResponseType(typeof(VerifyOTPResponse), 200)]
        [ProducesResponseType(typeof(VerifyOTPResponse), 400)]
        public async Task<ActionResult<VerifyOTPResponse>> VerifySetup([FromBody] VerifyOTPRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理裝置設定驗證請求，帳號: {AccountId}", request.AccountId);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("模型驗證失敗: {Errors}", errors);

                    return BadRequest(new VerifyOTPResponse
                    {
                        Success = false,
                        ErrorMessage = errors,
                        RemainingAttempts = 3
                    });
                }

                // 根據帳號取得使用者
                var user = await _userRepository.GetByEmailAsync(request.AccountId);
                if (user == null)
                {
                    _logger.LogWarning("找不到使用者: {Email}", request.AccountId);
                    return BadRequest(new VerifyOTPResponse
                    {
                        Success = false,
                        ErrorMessage = "使用者不存在",
                        RemainingAttempts = 0
                    });
                }

                // 驗證設定 - 需要取得最新建立的[未啟用]設備
                var userDevices = await _otpService.GetUserDevicesAsync(user.Id);
                var latestInactiveDevice = userDevices
                    .Where(d => !d.IsActive)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefault();

                if (latestInactiveDevice == null)
                {
                    _logger.LogWarning("找不到使用者 {UserId} 的未啟用設備", user.Id);
                    return BadRequest(new VerifyOTPResponse
                    {
                        Success = false,
                        ErrorMessage = "找不到待驗證的設備",
                        RemainingAttempts = 0
                    });
                }

                // 驗證設定
                var isValid = await _otpService.VerifySetupAsync(user.Id, latestInactiveDevice.DeviceName, request.Code);

                var response = new VerifyOTPResponse
                {
                    Success = isValid,
                    ErrorMessage = isValid ? null : "OTP 代碼無效，請重新掃描 QR Code",
                    RemainingAttempts = isValid ? 0 : 2
                };

                if (isValid)
                {
                    _logger.LogInformation("裝置設定驗證成功，使用者: {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("裝置設定驗證失敗，使用者: {UserId}", user.Id);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "裝置設定驗證過程中發生錯誤");
                return StatusCode(500, new VerifyOTPResponse
                {
                    Success = false,
                    ErrorMessage = "系統內部錯誤，請稍後再試",
                    RemainingAttempts = 0
                });
            }
        }

        #endregion

        /// <summary>
        /// 驗證 OTP 代碼
        /// </summary>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(VerifyOTPResponse), 200)]
        [ProducesResponseType(typeof(VerifyOTPResponse), 400)]
        public async Task<ActionResult<VerifyOTPResponse>> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理 OTP 驗證請求，帳號: {AccountId}", request.AccountId);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("模型驗證失敗: {Errors}", errors);
                    
                    return BadRequest(new VerifyOTPResponse
                    {
                        Success = false,
                        ErrorMessage = errors,
                        RemainingAttempts = 3
                    });
                }

                // 根據帳號取得使用者
                var user = await _userRepository.GetByEmailAsync(request.AccountId);
                if (user == null)
                {
                    _logger.LogWarning("找不到使用者: {Email}", request.AccountId);
                    return BadRequest(new VerifyOTPResponse
                    {
                        Success = false,
                        ErrorMessage = "使用者不存在",
                        RemainingAttempts = 0
                    });
                }

                // 驗證 OTP
                var isValid = await _otpService.VerifyCodeAsync(user.Id, request.Code);

                var response = new VerifyOTPResponse
                {
                    Success = isValid,
                    ErrorMessage = isValid ? null : "OTP 代碼無效或已過期",
                    RemainingAttempts = isValid ? 0 : 2
                };

                if (isValid)
                {
                    _logger.LogInformation("OTP 驗證成功，使用者: {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("OTP 驗證失敗，使用者: {UserId}", user.Id);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP 驗證過程中發生錯誤");
                return StatusCode(500, new VerifyOTPResponse
                {
                    Success = false,
                    ErrorMessage = "系統內部錯誤，請稍後再試",
                    RemainingAttempts = 0
                });
            }
        }

        /// <summary>
        /// 取得使用者已綁定的裝置清單
        /// </summary>
        [HttpGet("devices")]
        [ProducesResponseType(typeof(IEnumerable<DeviceInfo>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<DeviceInfo>>> GetUserDevices([FromQuery] Guid userId)
        {
            try
            {
                _logger.LogInformation("取得使用者裝置清單，使用者: {UserId}", userId);

                var devices = await _otpService.GetUserDevicesAsync(userId);
                
                var deviceInfos = devices.Select(d => new DeviceInfo
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    CreatedAt = d.CreatedAt,
                    LastUsedAt = d.LastUsedAt,
                    IsActive = d.IsActive
                });

                return Ok(deviceInfos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得裝置清單過程中發生錯誤");
                return StatusCode(500, "系統內部錯誤，請稍後再試");
            }
        }

        /// <summary>
        /// 解除裝置綁定
        /// </summary>
        [HttpDelete("devices/{deviceId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDevice([FromQuery] Guid userId, [FromRoute] Guid deviceId)
        {
            try
            {
                _logger.LogInformation("解除裝置綁定，使用者: {UserId}, 裝置: {DeviceId}", userId, deviceId);

                await _otpService.DeleteDeviceAsync(userId, deviceId);

                _logger.LogInformation("裝置解除綁定成功");
                return Ok(new { Success = true, Message = "裝置已成功解除綁定" });
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("找不到指定的裝置，使用者: {UserId}, 裝置: {DeviceId}", userId, deviceId);
                return NotFound(new { Success = false, Message = "找不到指定的裝置" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解除裝置綁定過程中發生錯誤");
                return StatusCode(500, new { Success = false, Message = "系統內部錯誤，請稍後再試" });
            }
        }

        /// <summary>
        /// 產生備援恢復碼
        /// </summary>
        [HttpPost("devices/recovery-codes")]
        [ProducesResponseType(typeof(RecoveryCodesResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<RecoveryCodesResponse>> GenerateRecoveryCodes(
            [FromQuery] Guid userId, 
            [FromQuery] int count = 10)
        {
            try
            {
                _logger.LogInformation("產生備援恢復碼，使用者: {UserId}, 數量: {Count}", userId, count);

                if (count <= 0 || count > 20)
                {
                    return BadRequest(new RecoveryCodesResponse
                    {
                        Success = false,
                        ErrorMessage = "恢復碼數量必須在 1-20 之間"
                    });
                }

                var recoveryCodes = await _otpService.GenerateBackupCodesAsync(userId, count);

                var response = new RecoveryCodesResponse
                {
                    Success = true,
                    RecoveryCodes = recoveryCodes.ToList()
                };

                _logger.LogInformation("備援恢復碼產生成功，使用者: {UserId}", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "產生備援恢復碼過程中發生錯誤");
                return StatusCode(500, new RecoveryCodesResponse
                {
                    Success = false,
                    ErrorMessage = "系統內部錯誤，請稍後再試"
                });
            }
        }

      
    }
}
