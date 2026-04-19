using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// POCO đại diện cho bảng "users" trong PostgreSQL.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
