using System;
using System.Data;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "images". Chỉ lưu metadata + file path; binary lưu vào disk.
    /// Phase 5 bổ sung SHA256 dedup + quota per user.
    /// </summary>
    public static class ImageRepository
    {
        public const int MaxImagesPerUser = 100;
        public const long MaxBytesPerUser = 500L * 1024L * 1024L; // 500 MB

        public static string Create(Image img)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<string>(
                    @"INSERT INTO images (user_id, file_name, url, mime_type, size, file_data, sha256_hash, doc_id)
                      VALUES (@UserId, @FileName, @Url, @MimeType, @Size, @FileData, @Sha256Hash, @DocId)
                      RETURNING id",
                    new
                    {
                        img.UserId,
                        img.FileName,
                        img.Url,
                        img.MimeType,
                        img.Size,
                        img.FileData,
                        img.Sha256Hash,
                        img.DocId
                    });
            }
        }

        public static Image GetById(string id)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<Image>(
                    @"SELECT id AS Id,
                             user_id AS UserId,
                             file_name AS FileName,
                             file_data AS FileData,
                             url AS Url,
                             mime_type AS MimeType,
                             size AS Size,
                             sha256_hash AS Sha256Hash,
                             doc_id AS DocId,
                             uploaded_at AS UploadedAt
                      FROM images
                      WHERE id = @Id",
                    new { Id = id });
            }
        }

        public static Image GetByHash(string sha256Hash)
        {
            if (string.IsNullOrWhiteSpace(sha256Hash)) return null;

            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<Image>(
                    @"SELECT id AS Id,
                             user_id AS UserId,
                             file_name AS FileName,
                             file_data AS FileData,
                             url AS Url,
                             mime_type AS MimeType,
                             size AS Size,
                             sha256_hash AS Sha256Hash,
                             doc_id AS DocId,
                             uploaded_at AS UploadedAt
                      FROM images
                      WHERE sha256_hash = @Hash
                      ORDER BY uploaded_at ASC
                      LIMIT 1",
                    new { Hash = sha256Hash });
            }
        }

        public static int CountByUserId(int userId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM images WHERE user_id = @UserId",
                    new { UserId = userId });
            }
        }

        public static long SumSizeByUserId(int userId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<long>(
                    "SELECT COALESCE(SUM(size), 0) FROM images WHERE user_id = @UserId",
                    new { UserId = userId });
            }
        }

        public static bool WouldExceedQuota(int userId, int newImageBytes)
        {
            if (newImageBytes < 0) newImageBytes = 0;
            int count = CountByUserId(userId);
            if (count >= MaxImagesPerUser) return true;

            long usedBytes = SumSizeByUserId(userId);
            return usedBytes + newImageBytes > MaxBytesPerUser;
        }

        public static bool CanUserAccessImage(int userId, string imageId)
        {
            var img = GetById(imageId);
            if (img == null) return false;
            if (img.UserId == userId) return true;
            if (string.IsNullOrWhiteSpace(img.DocId)) return false;

            string permission = MarkTogether.Server.Services.DocumentPermissionService.ResolvePermission(img.DocId, userId);
            return MarkTogether.Server.Services.DocumentPermissionService.CanRead(permission);
        }

        public static bool UpdateUserAvatar(int userId, string url)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "UPDATE users SET avatar_url = @Url WHERE id = @Id",
                    new { Url = url, Id = userId });
                return affected > 0;
            }
        }
    }
}