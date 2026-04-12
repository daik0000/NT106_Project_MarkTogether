using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// POCO đại diện cho bảng "document_versions" trong PostgreSQL.
    /// Lưu snapshot nội dung tại một thời điểm nhất định.
    /// </summary>
    public class DocumentVersion
    {
        public string Id { get; set; }
        public string DocId { get; set; }
        public string ContentSnapshot { get; set; }
        public int SavedBy { get; set; }
        public DateTime SavedAt { get; set; }
        public string Label { get; set; }
    }
}
