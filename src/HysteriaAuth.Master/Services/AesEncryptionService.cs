using System.Security.Cryptography;

namespace HysteriaAuth.Master.Services;

/// <summary>
/// AES-256-GCM 加密服务，用于节点密钥的安全存储。
/// 加密密钥从 appsettings.json 的 Encryption:MasterKey 或环境变量中读取。
/// </summary>
public class AesEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new ArgumentException("AES-256 密钥必须为 32 字节（Base64 编码）");
    }

    /// <summary>
    /// 使用 AES-256-GCM 加密明文，返回 Base64 编码的密文（格式: Nonce[12] + Tag[16] + Ciphertext）。
    /// </summary>
    public string Encrypt(string plaintext)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[12]; // 96-bit nonce
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16]; // 128-bit authentication tag

        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // 格式: Nonce + Tag + Ciphertext
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// 解密由 Encrypt 生成的密文，返回明文字符串。
    /// </summary>
    public string Decrypt(string ciphertext)
    {
        var data = Convert.FromBase64String(ciphertext);
        var nonce = data[..12];
        var tag = data[12..28];
        var cipherBytes = data[28..];

        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, tag.Length);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
