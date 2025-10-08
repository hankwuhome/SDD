using MOTPDualAuthWebsite.API.Models;
using MOTPDualAuthWebsite.API.Repositories;
using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace MOTPDualAuthWebsite.API.Services
{
    /// <summary>
    /// OTP 服務實作
    /// </summary>
    public class OTPService : IOTPService
    {
        private readonly IOTPRepository _otpRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBackupCodeRepository _backupCodeRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<OTPService> _logger;
        private readonly IConfiguration _configuration;

        public OTPService(
            IOTPRepository otpRepository,
            IUserRepository userRepository,
            IBackupCodeRepository backupCodeRepository,
            IEncryptionService encryptionService,
            ILogger<OTPService> logger,
            IConfiguration configuration)
        {
            _otpRepository = otpRepository;
            _userRepository = userRepository;
            _backupCodeRepository = backupCodeRepository;
            _encryptionService = encryptionService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 產生 QR Code 用於設備綁定
        /// </summary>
        public async Task<string> GenerateQRCodeAsync(Guid userId, string deviceName)
        {
            try
            {
                _logger.LogInformation("開始為使用者 {UserId} 產生設備 {DeviceName} 的 QR Code", userId, deviceName);

                // 檢查使用者是否存在
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("找不到使用者 {UserId}", userId);
                    throw new ArgumentException("使用者不存在");
                }

                // 檢查設備名稱是否已存在
                var existingDevice = await _otpRepository.GetByUserIdAndDeviceNameAsync(userId, deviceName);
                if (existingDevice != null)
                {
                    _logger.LogWarning("使用者 {UserId} 的設備 {DeviceName} 已存在", userId, deviceName);
                    throw new InvalidOperationException("設備名稱已存在");
                }

                // 檢查設備數量限制
                var userDevices = await _otpRepository.GetByUserIdAsync(userId);
                var maxDevices = _configuration.GetValue<int>("OTP:MaxDevices", 2);
                if (userDevices.Count() >= maxDevices)
                {
                    _logger.LogWarning("使用者 {UserId} 已達到設備數量上限 {MaxDevices}", userId, maxDevices);
                    throw new InvalidOperationException($"已達到設備數量上限 ({maxDevices})");
                }

                // 產生密鑰
                var secretKey = GenerateSecretKey();
                var encryptedSecretKey = await _encryptionService.EncryptAsync(secretKey);

                // 建立 OTP 記錄
                var otpCode = new OTPCode
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DeviceName = deviceName,
                    SecretKey = encryptedSecretKey,
                    IsActive = false, // 設置完成後才啟用
                    CreatedAt = DateTime.UtcNow
                };

                await _otpRepository.CreateAsync(otpCode);

                // 產生 QR Code
                var qrCodeData = GenerateQRCodeData(user.Email, secretKey, deviceName);

                _logger.LogInformation("成功為使用者 {UserId} 產生設備 {DeviceName} 的 QR Code", userId, deviceName);
                return qrCodeData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "為使用者 {UserId} 產生設備 {DeviceName} QR Code 時發生錯誤", userId, deviceName);
                throw;
            }
        }

        /// <summary>
        /// 驗證設備設置
        /// 通過(IsActive=1)，未通過((IsActive=0)
        /// </summary>
        public async Task<bool> VerifySetupAsync(Guid userId, string deviceName, string otpCode)
        {
            try
            {
                _logger.LogInformation("開始驗證使用者 {UserId} 設備 {DeviceName} 的設置", userId, deviceName);

                var device = await _otpRepository.GetByUserIdAndDeviceNameAsync(userId, deviceName);
                if (device == null)
                {
                    _logger.LogWarning("找不到使用者 {UserId} 的設備 {DeviceName}", userId, deviceName);
                    return false;
                }

                if (device.IsActive)
                {
                    _logger.LogWarning("使用者 {UserId} 的設備 {DeviceName} 已經啟用", userId, deviceName);
                    return false;
                }

                // 解密密鑰
                var secretKey = await _encryptionService.DecryptAsync(device.SecretKey);

                // 驗證 OTP 碼
                var isValid = await ValidateTOTPCodeAsync(secretKey, otpCode, 5);   //TODO時間差要改參數設定

                if (isValid)
                {
                    // 啟用設備
                    device.IsActive = true;
                    device.LastUsedAt = DateTime.UtcNow;
                    await _otpRepository.UpdateAsync(device);

                    _logger.LogInformation("使用者 {UserId} 設備 {DeviceName} 設置驗證成功", userId, deviceName);
                }
                else
                {
                    _logger.LogWarning("使用者 {UserId} 設備 {DeviceName} 設置驗證失敗", userId, deviceName);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證使用者 {UserId} 設備 {DeviceName} 設置時發生錯誤", userId, deviceName);
                return false;
            }
        }

        /// <summary>
        /// 驗證 OTP 驗證碼
        /// </summary>
        public async Task<bool> VerifyCodeAsync(Guid userId, string otpCode)
        {
            try
            {
                _logger.LogInformation("開始驗證使用者 {UserId} 的 OTP 驗證碼", userId);

                var devices = await _otpRepository.GetByUserIdAsync(userId);
                var activeDevices = devices.Where(d => d.IsActive).ToList();

                if (!activeDevices.Any())
                {
                    _logger.LogWarning("使用者 {UserId} 沒有啟用的設備", userId);
                    return false;
                }

                // 嘗試使用每個啟用的設備驗證
                foreach (var device in activeDevices)
                {
                    try
                    {
                        var secretKey = await _encryptionService.DecryptAsync(device.SecretKey);
                        var isValid = await ValidateTOTPCodeAsync(secretKey, otpCode, 5);   //TODO時間差要改參數設定

                        if (isValid)
                        {
                            // 更新最後使用時間
                            await _otpRepository.UpdateLastUsedAtAsync(device.Id);

                            _logger.LogInformation("使用者 {UserId} 使用設備 {DeviceName} 驗證成功", userId, device.DeviceName);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "驗證使用者 {UserId} 設備 {DeviceName} 時發生錯誤", userId, device.DeviceName);
                        continue;
                    }
                }

                // 檢查是否為備用驗證碼
                var backupCode = await _backupCodeRepository.GetByUserIdAndCodeAsync(userId, otpCode);
                if (backupCode != null && !backupCode.IsUsed)
                {
                    await _backupCodeRepository.MarkAsUsedAsync(backupCode.Id);
                    _logger.LogInformation("使用者 {UserId} 使用備用驗證碼驗證成功", userId);
                    return true;
                }

                _logger.LogWarning("使用者 {UserId} OTP 驗證碼驗證失敗", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證使用者 {UserId} OTP 驗證碼時發生錯誤", userId);
                return false;
            }
        }

        /// <summary>
        /// 取得使用者的所有設備
        /// </summary>
        public async Task<IEnumerable<OTPCode>> GetUserDevicesAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("取得使用者 {UserId} 的所有設備", userId);
                return await _otpRepository.GetByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者 {UserId} 設備清單時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 刪除設備
        /// </summary>
        public async Task DeleteDeviceAsync(Guid userId, Guid deviceId)
        {
            try
            {
                _logger.LogInformation("開始刪除使用者 {UserId} 的設備 {DeviceId}", userId, deviceId);

                var device = await _otpRepository.GetByIdAsync(deviceId);
                if (device == null || device.UserId != userId)
                {
                    _logger.LogWarning("找不到使用者 {UserId} 的設備 {DeviceId}", userId, deviceId);
                    throw new ArgumentException("設備不存在或無權限");
                }

                await _otpRepository.DeleteAsync(deviceId);

                _logger.LogInformation("成功刪除使用者 {UserId} 的設備 {DeviceId}", userId, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除使用者 {UserId} 設備 {DeviceId} 時發生錯誤", userId, deviceId);
                throw;
            }
        }

        /// <summary>
        /// 產生備用驗證碼
        /// </summary>
        public async Task<IEnumerable<string>> GenerateBackupCodesAsync(Guid userId, int count = 10)
        {
            try
            {
                _logger.LogInformation("開始為使用者 {UserId} 產生 {Count} 個備用驗證碼", userId, count);

                // 刪除舊的備用驗證碼
                await _backupCodeRepository.DeleteByUserIdAsync(userId);

                var backupCodes = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    var code = GenerateBackupCode();
                    backupCodes.Add(code);

                    var backupCode = new BackupCode
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Code = await _encryptionService.EncryptAsync(code),
                        IsUsed = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _backupCodeRepository.CreateAsync(backupCode);
                }

                _logger.LogInformation("成功為使用者 {UserId} 產生 {Count} 個備用驗證碼", userId, count);
                return backupCodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "為使用者 {UserId} 產生備用驗證碼時發生錯誤", userId);
                throw;
            }
        }

        /// <summary>
        /// 產生 TOTP 驗證碼
        /// </summary>
        public async Task<string> GenerateTOTPCodeAsync(string secretKey)
        {
            try
            {
                var keyBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(keyBytes);
                return await Task.FromResult(totp.ComputeTotp());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "產生 TOTP 驗證碼時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 驗證 TOTP 驗證碼
        /// </summary>
        /// <param name="secretKey">密鑰</param>
        /// <param name="code">是否在允許的時間窗口內正確</param>
        /// <param name="toleranceMinutes">容許 [前後 ± ?分鐘] 的時間誤差</param>
        /// <returns></returns>
        public async Task<bool> ValidateTOTPCodeAsync(string secretKey, string code, int toleranceMinutes = 5)
        {
            try
            {
                var keyBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(keyBytes);

                // 將分鐘數轉換成窗口數 (每窗口 30 秒)
                int windows = toleranceMinutes * 2;
                var window = new VerificationWindow(previous: windows, future: windows);

                return await Task.FromResult(totp.VerifyTotp(code, out _, window));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證 TOTP 驗證碼時發生錯誤");
                return false;
            }
        }
        //public async Task<bool> ValidateTOTPCodeAsync(string secretKey, string code)
        //{
        //    try
        //    {
        //        var keyBytes = Base32Encoding.ToBytes(secretKey);
        //        var totp = new Totp(keyBytes);

        //        // 擴大時間窗口誤差 (前後各三個時間窗口，總共7個窗口 = 3.5分鐘容錯)
        //        var window = new VerificationWindow(previous: 3, future: 3);
        //        return await Task.FromResult(totp.VerifyTotp(code, out _, window));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "驗證 TOTP 驗證碼時發生錯誤");
        //        return false;
        //    }
        //}

        #region Private Methods

        /// <summary>
        /// 產生密鑰
        /// </summary>
        private string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        /// <summary>
        /// 產生 QR Code 資料
        /// </summary>
        private string GenerateQRCodeData(string email, string secretKey, string deviceName)
        {
            var issuer = _configuration.GetValue<string>("OTP:Issuer", "MOTP Website");
            var label = $"{issuer}:{email}";

            var otpAuthUrl = $"otpauth://totp/{Uri.EscapeDataString(label)}?" +
                           $"secret={secretKey}&" +
                           $"issuer={Uri.EscapeDataString(issuer)}&" +
                           $"algorithm=SHA1&" +
                           $"digits=6&" +
                           $"period=30";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new Base64QRCode(qrCodeData);

            return qrCode.GetGraphic(20);
        }

        /// <summary>
        /// 產生備用驗證碼
        /// </summary>
        private string GenerateBackupCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[5];
            rng.GetBytes(bytes);

            var code = Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .ToUpper();

            return code.Substring(0, 8);
        }

        #endregion
    }
}