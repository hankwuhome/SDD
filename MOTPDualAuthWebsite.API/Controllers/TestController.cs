using Microsoft.AspNetCore.Mvc;
using MOTPDualAuthWebsite.API.Services;
using MOTPDualAuthWebsite.API.Repositories;
using OtpNet;
using System.Text;

namespace MOTPDualAuthWebsite.API.Controllers
{
    /// <summary>
    /// 測試控制器 - 用於測試各種功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IOTPService _otpService;
        private readonly IUserRepository _userRepository;
        private readonly IOTPRepository _otpRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<TestController> _logger;

        public TestController(
            IOTPService otpService,
            IUserRepository userRepository,
            IOTPRepository otpRepository,
            IEncryptionService encryptionService,
            ILogger<TestController> logger)
        {
            _otpService = otpService;
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        /// <summary>
        /// 測試 TOTP 驗證診斷
        /// </summary>
        [HttpPost("diagnose-totp")]
        public async Task<ActionResult> DiagnoseTOTP([FromBody] DiagnoseRequest request)
        {
            try
            {
                _logger.LogInformation("開始診斷 TOTP，使用者: {Email}", request.Email);

                // 1. 查找使用者
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { Error = "找不到使用者" });
                }

                // 2. 取得使用者的設備
                var devices = await _otpRepository.GetByUserIdAsync(user.Id);
                var latestDevice = devices.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
                
                if (latestDevice == null)
                {
                    return BadRequest(new { Error = "找不到設備" });
                }

                // 3. 解密密鑰
                var encryptedSecret = latestDevice.SecretKey;
                var decryptedSecret = await _encryptionService.DecryptAsync(encryptedSecret);

                // 4. 生成當前時間的 TOTP 碼
                var keyBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(keyBytes);
                var currentCode = totp.ComputeTotp();
                
                // 計算當前時間步驟
                var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var currentTimeStep = unixTime / 30; // TOTP 預設是 30 秒一個時間窗口
                
                // 5. 驗證提供的代碼
                var window = new VerificationWindow(previous: 2, future: 2);
                var isValid = totp.VerifyTotp(request.Code, out var timeStepMatched, window);

                // 6. 生成診斷資訊
                var diagnosis = new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    DeviceId = latestDevice.Id,
                    DeviceName = latestDevice.DeviceName,
                    IsActive = latestDevice.IsActive,
                    CreatedAt = latestDevice.CreatedAt,
                    EncryptedSecret = encryptedSecret,
                    DecryptedSecret = decryptedSecret,
                    CurrentServerTime = DateTime.UtcNow,
                    CurrentTimeStep = currentTimeStep,
                    GeneratedCode = currentCode,
                    ProvidedCode = request.Code,
                    IsValid = isValid,
                    TimeStepMatched = timeStepMatched,
                    Message = isValid ? "驗證成功" : "驗證失敗"
                };

                _logger.LogInformation("TOTP 診斷完成: {@Diagnosis}", diagnosis);
                return Ok(diagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOTP 診斷時發生錯誤");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 測試加密解密
        /// </summary>
        [HttpPost("test-encryption")]
        public async Task<ActionResult> TestEncryption([FromBody] TestEncryptionRequest request)
        {
            try
            {
                var encrypted = await _encryptionService.EncryptAsync(request.PlainText);
                var decrypted = await _encryptionService.DecryptAsync(encrypted);

                return Ok(new
                {
                    Original = request.PlainText,
                    Encrypted = encrypted,
                    Decrypted = decrypted,
                    IsMatch = request.PlainText == decrypted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "測試加密解密時發生錯誤");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 生成測試 TOTP 碼
        /// </summary>
        [HttpPost("generate-test-totp")]
        public ActionResult GenerateTestTOTP([FromBody] GenerateTOTPRequest request)
        {
            try
            {
                var keyBytes = Base32Encoding.ToBytes(request.SecretKey);
                var totp = new Totp(keyBytes);
                
                var codes = new List<object>();
                var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var currentTimeStep = unixTime / 30;
                
                // 生成前後幾個時間窗口的代碼
                for (int i = -2; i <= 2; i++)
                {
                    var timeStep = currentTimeStep + i;
                    var targetTime = DateTime.UtcNow.AddSeconds(i * 30);
                    var code = totp.ComputeTotp(targetTime);
                    codes.Add(new
                    {
                        TimeStep = timeStep,
                        Code = code,
                        Time = targetTime,
                        IsCurrent = i == 0
                    });
                }

                return Ok(new
                {
                    SecretKey = request.SecretKey,
                    CurrentTime = DateTime.UtcNow,
                    CurrentTimeStep = currentTimeStep,
                    Codes = codes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成測試 TOTP 碼時發生錯誤");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 快速驗證 - 使用伺服器當前生成的驗證碼
        /// </summary>
        [HttpPost("quick-verify")]
        public async Task<ActionResult> QuickVerify([FromBody] QuickVerifyRequest request)
        {
            try
            {
                _logger.LogInformation("開始快速驗證，使用者: {Email}", request.Email);

                // 1. 查找使用者
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { Error = "找不到使用者" });
                }

                // 2. 取得最新設備
                var devices = await _otpRepository.GetByUserIdAsync(user.Id);
                var latestDevice = devices.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
                
                if (latestDevice == null)
                {
                    return BadRequest(new { Error = "找不到設備" });
                }

                // 3. 解密密鑰並生成當前驗證碼
                var decryptedSecret = await _encryptionService.DecryptAsync(latestDevice.SecretKey);
                var keyBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(keyBytes);
                var currentCode = totp.ComputeTotp();

                // 4. 使用伺服器生成的驗證碼進行驗證設定
                var isValid = await _otpService.VerifySetupAsync(user.Id, latestDevice.DeviceName, currentCode);

                return Ok(new
                {
                    Message = isValid ? "驗證設定成功！設備已啟用" : "驗證設定失敗",
                    Success = isValid,
                    ServerGeneratedCode = currentCode,
                    DeviceName = latestDevice.DeviceName,
                    IsNowActive = isValid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速驗證時發生錯誤");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 詳細驗證設定診斷
        /// </summary>
        [HttpPost("debug-verify-setup")]
        public async Task<ActionResult> DebugVerifySetup([FromBody] QuickVerifyRequest request)
        {
            try
            {
                _logger.LogInformation("開始詳細驗證設定診斷，使用者: {Email}", request.Email);

                // 1. 查找使用者
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { Error = "找不到使用者" });
                }

                // 2. 取得所有設備
                var allDevices = await _otpRepository.GetByUserIdAsync(user.Id);
                var latestDevice = allDevices.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
                
                if (latestDevice == null)
                {
                    return BadRequest(new { Error = "找不到設備" });
                }

                // 3. 嘗試用設備名稱查找
                var deviceByName = await _otpRepository.GetByUserIdAndDeviceNameAsync(user.Id, latestDevice.DeviceName);
                
                // 4. 解密密鑰並生成驗證碼
                var decryptedSecret = await _encryptionService.DecryptAsync(latestDevice.SecretKey);
                var keyBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(keyBytes);
                var currentCode = totp.ComputeTotp();

                // 5. 直接測試 TOTP 驗證
                var window = new VerificationWindow(previous: 3, future: 3);
                var totpValid = totp.VerifyTotp(currentCode, out var timeStepMatched, window);

                // 6. 測試驗證設定邏輯的每一步
                var debugInfo = new
                {
                    Step1_UserFound = user != null,
                    Step2_AllDevicesCount = allDevices.Count(),
                    Step3_LatestDevice = new
                    {
                        latestDevice.Id,
                        latestDevice.DeviceName,
                        latestDevice.IsActive,
                        latestDevice.CreatedAt
                    },
                    Step4_DeviceByName = deviceByName != null ? new
                    {
                        Found = true,
                        Id = (Guid?)deviceByName.Id,
                        DeviceName = (string?)deviceByName.DeviceName,
                        IsActive = (bool?)deviceByName.IsActive
                    } : new { Found = false, Id = (Guid?)null, DeviceName = (string?)null, IsActive = (bool?)null },
                    Step5_DecryptedSecret = decryptedSecret,
                    Step6_GeneratedCode = currentCode,
                    Step7_DirectTOTPTest = new
                    {
                        IsValid = totpValid,
                        TimeStepMatched = timeStepMatched
                    },
                    Step8_IsDeviceActive = latestDevice.IsActive,
                    Step9_SameDevice = latestDevice.Id == deviceByName?.Id
                };

                // 7. 如果設備未啟用且找到了，嘗試手動更新
                if (deviceByName != null && !deviceByName.IsActive && totpValid)
                {
                    deviceByName.IsActive = true;
                    deviceByName.LastUsedAt = DateTime.UtcNow;
                    await _otpRepository.UpdateAsync(deviceByName);
                    
                    return Ok(new
                    {
                        Message = "驗證設定成功！設備已手動啟用",
                        Success = true,
                        DebugInfo = debugInfo,
                        ManuallyActivated = true
                    });
                }

                return Ok(new
                {
                    Message = "詳細診斷完成",
                    Success = false,
                    DebugInfo = debugInfo,
                    PossibleIssue = deviceByName == null ? "找不到設備" : 
                                   deviceByName.IsActive ? "設備已啟用" : 
                                   !totpValid ? "TOTP驗證失敗" : "未知問題"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "詳細驗證設定診斷時發生錯誤");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// 根據使用者ID診斷
        /// </summary>
        [HttpPost("diagnose-by-userid")]
        public async Task<ActionResult> DiagnoseByUserId([FromBody] DiagnoseByUserIdRequest request)
        {
            try
            {
                _logger.LogInformation("開始根據使用者ID診斷，使用者ID: {UserId}", request.UserId);

                // 1. 查找使用者
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return BadRequest(new { Error = "找不到使用者" });
                }

                // 2. 取得所有設備
                var allDevices = await _otpRepository.GetByUserIdAsync(request.UserId);
                
                var deviceDetails = allDevices.Select(d => new
                {
                    d.Id,
                    d.DeviceName,
                    d.IsActive,
                    d.CreatedAt,
                    d.LastUsedAt
                }).ToList();

                // 3. 找到指定的設備
                var targetDevice = allDevices.FirstOrDefault(d => d.DeviceName == request.DeviceName);
                
                if (targetDevice == null)
                {
                    return Ok(new
                    {
                        Message = "找不到指定設備",
                        UserId = request.UserId,
                        UserEmail = user.Email,
                        RequestedDeviceName = request.DeviceName,
                        AllDevices = deviceDetails
                    });
                }

                // 4. 解密密鑰並測試
                var decryptedSecret = await _encryptionService.DecryptAsync(targetDevice.SecretKey);
                var keyBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(keyBytes);
                var currentCode = totp.ComputeTotp();

                // 5. 測試不同的時間窗口
                var window = new VerificationWindow(previous: 3, future: 3);
                var testResults = new List<object>();
                
                for (int i = -3; i <= 3; i++)
                {
                    var testTime = DateTime.UtcNow.AddSeconds(i * 30);
                    var testCode = totp.ComputeTotp(testTime);
                    var isValid = totp.VerifyTotp(testCode, out var timeStep, window);
                    
                    testResults.Add(new
                    {
                        TimeOffset = i,
                        TestTime = testTime,
                        GeneratedCode = testCode,
                        IsValid = isValid,
                        TimeStep = timeStep,
                        IsCurrent = i == 0
                    });
                }

                return Ok(new
                {
                    Message = "診斷完成",
                    UserInfo = new
                    {
                        UserId = user.Id,
                        Email = user.Email
                    },
                    TargetDevice = new
                    {
                        targetDevice.Id,
                        targetDevice.DeviceName,
                        targetDevice.IsActive,
                        targetDevice.CreatedAt,
                        targetDevice.LastUsedAt
                    },
                    DecryptedSecret = decryptedSecret,
                    CurrentServerCode = currentCode,
                    TimeWindowTests = testResults,
                    AllDevices = deviceDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據使用者ID診斷時發生錯誤");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// 自動啟用設備 - 使用伺服器生成的驗證碼
        /// </summary>
        [HttpPost("auto-activate-device")]
        public async Task<ActionResult> AutoActivateDevice([FromBody] DiagnoseByUserIdRequest request)
        {
            try
            {
                _logger.LogInformation("開始自動啟用設備，使用者ID: {UserId}, 設備: {DeviceName}", request.UserId, request.DeviceName);

                // 1. 查找使用者
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return BadRequest(new { Error = "找不到使用者" });
                }

                // 2. 找到指定的設備
                var device = await _otpRepository.GetByUserIdAndDeviceNameAsync(request.UserId, request.DeviceName);
                if (device == null)
                {
                    return BadRequest(new { Error = "找不到指定設備" });
                }

                if (device.IsActive)
                {
                    return Ok(new
                    {
                        Message = "設備已經啟用",
                        Success = true,
                        DeviceName = device.DeviceName,
                        IsActive = device.IsActive
                    });
                }

                // 3. 解密密鑰並生成當前驗證碼
                var decryptedSecret = await _encryptionService.DecryptAsync(device.SecretKey);
                var keyBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(keyBytes);
                var currentCode = totp.ComputeTotp();

                // 4. 使用伺服器生成的驗證碼進行驗證設定
                var isValid = await _otpService.VerifySetupAsync(request.UserId, request.DeviceName, currentCode);

                if (isValid)
                {
                    return Ok(new
                    {
                        Message = "設備自動啟用成功！",
                        Success = true,
                        DeviceName = device.DeviceName,
                        UsedCode = currentCode,
                        UserEmail = user.Email,
                        IsNowActive = true
                    });
                }
                else
                {
                    return Ok(new
                    {
                        Message = "自動啟用失敗，請手動驗證",
                        Success = false,
                        DeviceName = device.DeviceName,
                        ServerGeneratedCode = currentCode,
                        UserEmail = user.Email,
                        Suggestion = $"請在 TOTP 應用程式中使用驗證碼 {currentCode} 或重新掃描 QR Code"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自動啟用設備時發生錯誤");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        public class DiagnoseRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        public class TestEncryptionRequest
        {
            public string PlainText { get; set; } = string.Empty;
        }

        public class GenerateTOTPRequest
        {
            public string SecretKey { get; set; } = string.Empty;
        }

        public class QuickVerifyRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class DiagnoseByUserIdRequest
        {
            public Guid UserId { get; set; }
            public string DeviceName { get; set; } = string.Empty;
        }
    }
} 
