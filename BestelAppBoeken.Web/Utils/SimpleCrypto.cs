using System;
using System.Security.Cryptography;
using System.Text;

namespace BestelAppBoeken.Web.Utils
{
    public static class SimpleCrypto
    {
        // Derive a 32-byte key from environment variable or fallback to a fixed demo key.
        private static byte[] GetKey()
        {
            var key = Environment.GetEnvironmentVariable("APP_CRYPTO_KEY") ?? "demo-demo-demo-demo-demo-demo-32bytes!";
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var k = new byte[32];
            Array.Clear(k, 0, k.Length);
            Array.Copy(keyBytes, k, Math.Min(keyBytes.Length, k.Length));
            return k;
        }

        public static string Encrypt(string plainText)
        {
            if (plainText == null) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = GetKey();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string encryptedBase64)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(encryptedBase64)) return string.Empty;
                var combined = Convert.FromBase64String(encryptedBase64);

                using var aes = Aes.Create();
                aes.Key = GetKey();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var ivLength = aes.BlockSize / 8;
                if (combined.Length < ivLength) return string.Empty;

                var iv = new byte[ivLength];
                Array.Copy(combined, 0, iv, 0, ivLength);
                var cipher = new byte[combined.Length - ivLength];
                Array.Copy(combined, ivLength, cipher, 0, cipher.Length);

                using var decryptor = aes.CreateDecryptor(aes.Key, iv);
                var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            Span<byte> buffer = new byte[s.Length];
            return Convert.TryFromBase64String(s, buffer, out _);
        }
    }
}
