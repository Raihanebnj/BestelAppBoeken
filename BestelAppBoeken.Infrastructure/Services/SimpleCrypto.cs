// BestelAppBoeken.Infrastructure/Services/SimpleCrypto.cs
using System.Security.Cryptography;
using System.Text;

namespace BestelAppBoeken.Infrastructure.Services
{
    public static class SimpleCrypto
    {
        // BELANGRIJK: exact 32 characters voor AES-256
        private static readonly string Key = "EhB-2026-Groep3-BestelAppDemoKey32!!";

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            try
            {
                using var aes = Aes.Create();

                // Zorg voor exact 32 bytes (256-bit)
                byte[] keyBytes = new byte[32];
                byte[] sourceBytes = Encoding.UTF8.GetBytes(Key);
                Array.Copy(sourceBytes, keyBytes, Math.Min(sourceBytes.Length, 32));

                aes.Key = keyBytes;
                aes.IV = new byte[16]; // 128-bit IV

                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Encryptie fout: {ex.Message}");
                return plainText; // Fallback naar plain text
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                // Als het al plain text is, retourneer direct
                if (!IsBase64(cipherText)) return cipherText;

                using var aes = Aes.Create();

                byte[] keyBytes = new byte[32];
                byte[] sourceBytes = Encoding.UTF8.GetBytes(Key);
                Array.Copy(sourceBytes, keyBytes, Math.Min(sourceBytes.Length, 32));

                aes.Key = keyBytes;
                aes.IV = new byte[16];

                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                // Als decryptie faalt, is het waarschijnlijk plain text
                return cipherText;
            }
        }

        private static bool IsBase64(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length % 4 != 0)
                return false;

            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}