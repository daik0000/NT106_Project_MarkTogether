using System;
using System.Security.Cryptography;

namespace MarkTogether.Server.Services
{
    public static class SecureTokenGenerator
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateUrlSafeToken(int length = 48)
        {
            if (length < 32) length = 32;
            char[] chars = new char[length];
            byte[] bytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = Alphabet[bytes[i] % Alphabet.Length];
            }

            return new string(chars);
        }
    }
}