using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// POCO đại diện cho bảng "document_shares" trong PostgreSQL.
    /// Quản lý quyền truy cập tài liệu (owner / editor / viewer).
    /// </summary>
    public class DocumentShare
    {
        public int Id { get; set; }
        public string DocId { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// Quyền truy cập: "owner", "editor", "viewer"
        /// </summary>
        public string Permission { get; set; }

        public DateTime InvitedAt { get; set; }
    }
}
