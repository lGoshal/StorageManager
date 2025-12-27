using System;
using System.Security.Cryptography;
using System.Text;

namespace StorageManager.Helpers
{
    /// <summary>
    /// Логика взаимодействия для PasswordHasher.cs
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Создает соль для пароля
        /// </summary>
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }
        /// <summary>
        /// Хэширует пароль с солью используя SHA-256
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            string saltedPassword = password + salt;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(saltedPassword);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
        /// <summary>
        /// Проверяет пароль
        /// </summary>
        public static bool VerifyPassword(string password, string salt, string storedHash)
        {
            string hashedPassword = HashPassword(password, salt);

            return CryptographicEquals(hashedPassword, storedHash);
        }
        /// <summary>
        /// Сравнение строк с постоянным временем выполнения
        /// </summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
        /// <summary>
        /// Миграция: хэширует старый пароль и создает соль
        /// </summary>
        public static (string hash, string salt) MigrateOldPassword(string plainPassword)
        {
            string salt = GenerateSalt();
            string hash = HashPassword(plainPassword, salt);
            return (hash, salt);
        }
    }
}