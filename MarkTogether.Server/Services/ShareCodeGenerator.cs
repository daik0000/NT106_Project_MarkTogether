using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MarkTogether.Server.Database.Repositories;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Sinh share code 8 ký tự (A-Z, 0-9), đảm bảo unique trong DB.
    /// </summary>
    public static class ShareCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // bỏ I O 0 1 dễ nhầm
        private const int CodeLength = 8;
        private const int MaxRetries = 10;

        /// <summary>
        /// Sinh code mới chưa trùng. Nếu thử quá nhiều lần đều trùng → ném exception.
        /// </summary>
        public static string GenerateUnique()
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                string code = Generate();
                if (!DocumentRepository.ShareCodeExists(code))
                {
                    return code;
                }
            }
            throw new InvalidOperationException(
                "Không sinh được share code unique sau nhiều lần thử.");
        }

        public static string Generate()
        {
            var sb = new StringBuilder(CodeLength);
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[CodeLength];
                rng.GetBytes(buffer);
                for (int i = 0; i < CodeLength; i++)
                {
                    sb.Append(Alphabet[buffer[i] % Alphabet.Length]);
                }
            }
            return sb.ToString();
        }
    }
}