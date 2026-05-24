using System;
using System.IO;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Lưu / đọc file ảnh trên disk: Server/Storage/images/{userId}/{guid}.{ext}
    /// </summary>
    public static class ImageStorageService
    {
        public const int MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly string _storageRoot = ResolveStorageRoot();

        private static readonly string _baseDir = Path.Combine(_storageRoot, "images");

        public static string Save(int userId, string fileName, byte[] data, string mimeType)
        {
            if (data == null || data.Length == 0)
                throw new InvalidOperationException("Dữ liệu ảnh rỗng.");
            if (data.Length > MaxImageBytes)
                throw new InvalidOperationException($"Ảnh vượt quá {MaxImageBytes / (1024 * 1024)} MB.");

            string normalizedMime = ImageValidator.NormalizeMime(mimeType);
            string ext = SanitizeExtension(fileName, normalizedMime);
            string userDir = Path.Combine(_baseDir, userId.ToString());
            Directory.CreateDirectory(userDir);

            string id = Guid.NewGuid().ToString("N");
            string fullPath = Path.Combine(userDir, id + ext);
            File.WriteAllBytes(fullPath, data);

            // Trả về relative path để lưu DB. Client đọc lại bằng IMAGE_GET nếu cần.
            return $"images/{userId}/{id}{ext}";
        }

        public static Image SaveWithDedup(int userId, string fileName, byte[] data, string mimeType, string docId)
        {
            var validation = ImageValidator.Validate(data, mimeType, fileName);
            if (!validation.Item1)
                throw new InvalidOperationException(validation.Item2);

            string normalizedMime = validation.Item3;
            string hash = ImageValidator.ComputeSha256(data);
            Image existing = ImageRepository.GetByHash(hash);
            if (existing != null)
                return existing;

            if (ImageRepository.WouldExceedQuota(userId, data.Length))
                throw new InvalidOperationException($"Vượt quota ảnh: tối đa {ImageRepository.MaxImagesPerUser} ảnh hoặc {ImageRepository.MaxBytesPerUser / (1024 * 1024)} MB mỗi người dùng.");

            string safeFileName = SanitizeFileName(fileName, normalizedMime);
            string url = Save(userId, safeFileName, data, normalizedMime);

            var image = new Image
            {
                UserId = userId,
                FileName = safeFileName,
                MimeType = normalizedMime,
                Url = url,
                Size = data.Length,
                Sha256Hash = hash,
                DocId = string.IsNullOrWhiteSpace(docId) ? null : docId.Trim()
            };
            image.Id = ImageRepository.Create(image);
            return image;
        }

        public static byte[] Load(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return null;

            string full = Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : Path.Combine(_storageRoot, relativeOrAbsolutePath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(full) ? File.ReadAllBytes(full) : null;
        }

        private static string ResolveStorageRoot()
        {
            string configuredRoot = Environment.GetEnvironmentVariable("MARKTOGETHER_STORAGE_PATH");
            if (!string.IsNullOrWhiteSpace(configuredRoot))
                return Path.GetFullPath(configuredRoot.Trim());

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");
        }

        private static string SanitizeExtension(string fileName, string mimeType)
        {
            string ext = string.IsNullOrEmpty(fileName) ? "" : Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(ext))
            {
                ext = ext.ToLowerInvariant();
                string expectedExt = ImageValidator.ExtensionFromMime(mimeType);
                return ext == ".jpeg" && expectedExt == ".jpg" ? ".jpg" : expectedExt;
            }

            return ImageValidator.ExtensionFromMime(mimeType);
        }

        private static string SanitizeFileName(string fileName, string mimeType)
        {
            string name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name)) name = "image";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Trim();
            if (name.Length > 80) name = name.Substring(0, 80);

            return name + ImageValidator.ExtensionFromMime(mimeType);
        }
    }
}