using System.Security.Cryptography;
using System.Text;

namespace MOTPDualAuthWebsite.API.Services
{
    /// <summary>
    /// 加密服務實作
    /// 使用 AES 對稱加密
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EncryptionService> _logger;
        private readonly byte[] _key;

        public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            var keyString = _configuration.GetValue<string>("Encryption:Key");
            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException("加密金鑰未設定");
            }
            
            _key = Encoding.UTF8.GetBytes(keyString.Substring(0, 32)); // AES-256 需要 32 bytes
        }

        /// <summary>
        /// 加密字串
        /// </summary>
        public async Task<string> EncryptAsync(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    return string.Empty;
                }

                using var aes = Aes.Create();
                aes.Key = _key;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);

                await swEncrypt.WriteAsync(plainText);
                swEncrypt.Close();

                var iv = aes.IV;
                var encrypted = msEncrypt.ToArray();
                var result = new byte[iv.Length + encrypted.Length];

                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加密字串時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 解密字串
        /// </summary>
        public async Task<string> DecryptAsync(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText))
                {
                    return string.Empty;
                }

                var fullCipher = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = _key;

                var iv = new byte[aes.IV.Length];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var msDecrypt = new MemoryStream(cipher);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return await srDecrypt.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解密字串時發生錯誤");
                throw;
            }
        }
    }
} 