using System;
using System.IO;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Lưu / đọc file ảnh trên disk: Server/Storage/images/{userId}/{guid}.{ext}
    /// </summary>
    public static class ImageStorageService
    {
        public const int MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly string _baseDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Storage", "images");

        public static string Save(int userId, string fileName, byte[] data, string mimeType)
        {
            if (data == null || data.Length == 0)
                throw new InvalidOperationException("Dữ liệu ảnh rỗng.");
            if (data.Length > MaxImageBytes)
                throw new InvalidOperationException($"Ảnh vượt quá {MaxImageBytes / (1024 * 1024)} MB.");

            string ext = SanitizeExtension(fileName, mimeType);
            string userDir = Path.Combine(_baseDir, userId.ToString());
            Directory.CreateDirectory(userDir);

            string id = Guid.NewGuid().ToString("N");
            string fullPath = Path.Combine(userDir, id + ext);
            File.WriteAllBytes(fullPath, data);

            // Trả về relative path để lưu DB. Client đọc lại bằng IMAGE_GET nếu cần.
            return $"images/{userId}/{id}{ext}";
        }

        public static byte[] Load(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return null;

            string full = Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage",
                    relativeOrAbsolutePath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(full) ? File.ReadAllBytes(full) : null;
        }

        private static string SanitizeExtension(string fileName, string mimeType)
        {
            string ext = string.IsNullOrEmpty(fileName) ? "" : Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(ext)) return ext.ToLowerInvariant();

            switch ((mimeType ?? "").ToLowerInvariant())
            {
                case "image/png": return ".png";
                case "image/jpeg":
                case "image/jpg": return ".jpg";
                case "image/gif": return ".gif";
                case "image/webp": return ".webp";
                case "image/bmp": return ".bmp";
                default: return ".bin";
            }
        }
    }
}