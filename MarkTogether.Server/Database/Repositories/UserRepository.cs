using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "users" — đăng ký, đăng nhập, tìm kiếm user.
    /// Password phải được hash bằng BCrypt TRƯỚC khi gọi Create().
    /// </summary>
    public static class UserRepository
    {
        /// <summary>
        /// Tìm user theo username (phục vụ đăng nhập).
        /// </summary>
        public static User GetByUsername(string username)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<User>(
                    "SELECT * FROM users WHERE username = @Username",
                    new { Username = username });
            }
        }

        /// <summary>
        /// Tìm user theo email.
        /// </summary>
        public static User GetByEmail(string email)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<User>(
                    "SELECT * FROM users WHERE email = @Email",
                    new { Email = email });
            }
        }

        /// <summary>
        /// Lấy thông tin user theo ID.
        /// </summary>
        public static User GetById(int id)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<User>(
                    "SELECT * FROM users WHERE id = @Id",
                    new { Id = id });
            }
        }

        /// <summary>
        /// Tạo user mới. Trả về ID của user vừa tạo.
        /// LƯU Ý: PasswordHash phải được hash bằng BCrypt trước khi truyền vào.
        /// </summary>
        public static int Create(User user)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    @"INSERT INTO users (username, email, password_hash, avatar_url)
                      VALUES (@Username, @Email, @PasswordHash, @AvatarUrl)
                      RETURNING id",
                    new
                    {
                        user.Username,
                        user.Email,
                        user.PasswordHash,
                        user.AvatarUrl
                    });
            }
        }

        /// <summary>
        /// Cập nhật username và avatar_url.
        /// </summary>
        public static bool Update(User user)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE users
                      SET username = @Username, avatar_url = @AvatarUrl
                      WHERE id = @Id",
                    new { user.Username, user.AvatarUrl, user.Id });
                return affected > 0;
            }
        }

        /// <summary>
        /// Đổi mật khẩu (cập nhật password_hash).
        /// Hash mới phải được tạo bằng BCrypt trước khi gọi hàm này.
        /// </summary>
        public static bool UpdatePassword(int id, string newPasswordHash)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "UPDATE users SET password_hash = @Hash WHERE id = @Id",
                    new { Hash = newPasswordHash, Id = id });
                return affected > 0;
            }
        }

        /// <summary>
        /// Tìm kiếm user theo username hoặc email (dùng ILIKE cho case-insensitive).
        /// Phục vụ tính năng chia sẻ tài liệu.
        /// </summary>
        public static List<User> Search(string query, int limit = 10)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                string pattern = "%" + query + "%";
                return db.Query<User>(
                    @"SELECT id, username, email, avatar_url, created_at
                      FROM users
                      WHERE username ILIKE @Pattern OR email ILIKE @Pattern
                      LIMIT @Limit",
                    new { Pattern = pattern, Limit = limit }).ToList();
            }
        }
    }
}
