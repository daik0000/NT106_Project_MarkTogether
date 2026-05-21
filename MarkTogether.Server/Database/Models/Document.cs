using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// POCO đại diện cho bảng "documents" trong PostgreSQL.
    /// </summary>
    public class Document
    {
        public string Id { get; set; }
        public int OwnerId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string FilePathServer { get; set; }
        public string ShareCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
