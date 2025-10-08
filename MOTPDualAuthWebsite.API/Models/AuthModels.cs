using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MOTPDualAuthWebsite.API.Models
{
    /// <summary>
    /// 登入請求模型
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "電子郵件為必填欄位")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
        [StringLength(255, ErrorMessage = "電子郵件長度不能超過 255 個字元")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼為必填欄位")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "密碼長度必須在 6-255 個字元之間")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 記住登入狀態 (可選)
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// 登入回應模型
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// 登入是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 是否需要 TOTP 驗證
        /// </summary>
        public bool RequiresTOTP { get; set; }

        /// <summary>
        /// 使用者 ID (需要 TOTP 時提供)
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// JWT Token (完整認證後提供)
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// 使用者基本資訊
        /// </summary>
        public UserInfo? User { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 剩餘嘗試次數 (帳號鎖定相關)
        /// </summary>
        public int? RemainingAttempts { get; set; }

        /// <summary>
        /// 帳號鎖定到期時間
        /// </summary>
        public DateTime? LockedUntil { get; set; }
    }

    /// <summary>
    /// TOTP 驗證請求模型
    /// </summary>
    public class TOTPVerifyRequest
    {
        [Required(ErrorMessage = "使用者 ID 為必填欄位")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "驗證碼為必填欄位")]
        [StringLength(10, MinimumLength = 6, ErrorMessage = "驗證碼長度必須在 6-10 個字元之間")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 是否為備用驗證碼
        /// </summary>
        public bool IsBackupCode { get; set; } = false;
    }

    /// <summary>
    /// TOTP 驗證回應模型
    /// </summary>
    public class TOTPVerifyResponse
    {
        /// <summary>
        /// 驗證是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// JWT Token (驗證成功後提供)
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// 使用者基本資訊
        /// </summary>
        public UserInfo? User { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 剩餘嘗試次數
        /// </summary>
        public int? RemainingAttempts { get; set; }
    }

    /// <summary>
    /// 使用者基本資訊模型
    /// </summary>
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool HasTOTPEnabled { get; set; }
        public int ActiveDevicesCount { get; set; }
    }

    /// <summary>
    /// 認證結果模型 (內部使用)
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// 認證是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 使用者物件
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 剩餘嘗試次數
        /// </summary>
        public int RemainingAttempts { get; set; }

        /// <summary>
        /// 帳號鎖定到期時間
        /// </summary>
        public DateTime? LockedUntil { get; set; }

        /// <summary>
        /// 是否需要 TOTP 驗證
        /// </summary>
        public bool RequiresTOTP { get; set; }
    }

    /// <summary>
    /// 會話資訊模型
    /// </summary>
    public class SessionInfo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string JwtTokenId { get; set; } = string.Empty;

        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        public DateTime LoginAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LogoutAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    /// <summary>
    /// 登出請求模型
    /// </summary>
    public class LogoutRequest
    {
        /// <summary>
        /// 是否登出所有裝置
        /// </summary>
        public bool LogoutAllDevices { get; set; } = false;
    }

    /// <summary>
    /// 登出回應模型
    /// </summary>
    public class LogoutResponse
    {
        /// <summary>
        /// 登出是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
} 