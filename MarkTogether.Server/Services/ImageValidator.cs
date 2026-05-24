using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MarkTogether.Server.Services
{
    public static class ImageValidator
    {
        private static readonly HashSet<string> AllowedMimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/bmp"
        };

        public static string NormalizeMime(string mimeType)
        {
            string mime = (mimeType ?? string.Empty).Trim().ToLowerInvariant();
            return mime == "image/jpg" ? "image/jpeg" : mime;
        }

        public static Tuple<bool, string, string> Validate(byte[] data, string claimedMime, string fileName)
        {
            if (data == null || data.Length == 0)
                return Tuple.Create(false, "Dữ liệu ảnh rỗng.", (string)null);

            if (data.Length > ImageStorageService.MaxImageBytes)
                return Tuple.Create(false, $"Ảnh vượt quá {ImageStorageService.MaxImageBytes / (1024 * 1024)} MB.", (string)null);

            string mime = NormalizeMime(claimedMime);
            if (!AllowedMimes.Contains(mime))
                return Tuple.Create(false, "Định dạng ảnh không được hỗ trợ.", (string)null);

            if (!MatchesMagicBytes(data, mime))
                return Tuple.Create(false, "Nội dung file không khớp với định dạng ảnh khai báo.", (string)null);

            string ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && !IsExtensionValid(ext, mime))
                return Tuple.Create(false, "Phần mở rộng file không khớp MIME type.", (string)null);

            return Tuple.Create(true, (string)null, mime);
        }

        public static string ComputeSha256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        public static string ExtensionFromMime(string mime)
        {
            switch (NormalizeMime(mime))
            {
                case "image/png": return ".png";
                case "image/jpeg": return ".jpg";
                case "image/gif": return ".gif";
                case "image/webp": return ".webp";
                case "image/bmp": return ".bmp";
                default: return ".bin";
            }
        }

        private static bool MatchesMagicBytes(byte[] data, string mime)
        {
            if (mime == "image/png")
                return StartsWith(data, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            if (mime == "image/jpeg")
                return StartsWith(data, new byte[] { 0xFF, 0xD8, 0xFF });
            if (mime == "image/gif")
                return StartsWith(data, new byte[] { 0x47, 0x49, 0x46, 0x38 });
            if (mime == "image/bmp")
                return StartsWith(data, new byte[] { 0x42, 0x4D });
            if (mime == "image/webp")
                return data.Length >= 12
                    && StartsWith(data, new byte[] { 0x52, 0x49, 0x46, 0x46 })
                    && data.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 });
            return false;
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            return data.Length >= prefix.Length && data.Take(prefix.Length).SequenceEqual(prefix);
        }

        private static bool IsExtensionValid(string ext, string mime)
        {
            switch (NormalizeMime(mime))
            {
                case "image/png": return ext == ".png";
                case "image/jpeg": return ext == ".jpg" || ext == ".jpeg";
                case "image/gif": return ext == ".gif";
                case "image/webp": return ext == ".webp";
                case "image/bmp": return ext == ".bmp";
                default: return false;
            }
        }
    }
}