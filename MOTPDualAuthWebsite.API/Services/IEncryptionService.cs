namespace MOTPDualAuthWebsite.API.Services
{
    /// <summary>
    /// 加密服務介面
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// 加密字串
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>密文</returns>
        Task<string> EncryptAsync(string plainText);

        /// <summary>
        /// 解密字串
        /// </summary>
        /// <param name="cipherText">密文</param>
        /// <returns>明文</returns>
        Task<string> DecryptAsync(string cipherText);
    }
} 