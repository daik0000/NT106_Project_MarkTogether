using System.Data;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "images". Chỉ lưu metadata + file path; binary lưu vào disk.
    /// </summary>
    public static class ImageRepository
    {
        public static string Create(Image img)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<string>(
                    @"INSERT INTO images (user_id, file_name, url, mime_type, size, file_data)
                      VALUES (@UserId, @FileName, @Url, @MimeType, @Size, @FileData)
                      RETURNING id",
                    new
                    {
                        img.UserId,
                        img.FileName,
                        img.Url,
                        img.MimeType,
                        img.Size,
                        img.FileData
                    });
            }
        }

        public static Image GetById(string id)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<Image>(
                    "SELECT * FROM images WHERE id = @Id",
                    new { Id = id });
            }
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