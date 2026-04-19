using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// POCO đại diện cho bảng "document_operations" trong PostgreSQL.
    /// Lưu trữ từng thao tác chỉnh sửa (Operational Transformation).
    /// </summary>
    public class DocumentOperation
    {
        public string Id { get; set; }
        public string DocId { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// Loại thao tác: "insert" hoặc "delete"
        /// </summary>
        public string OpType { get; set; }

        /// <summary>
        /// Vị trí con trỏ trong document
        /// </summary>
        public int Pos { get; set; }

        /// <summary>
        /// Nội dung chèn (dùng cho op_type = "insert")
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Số ký tự cần xóa (dùng cho op_type = "delete")
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Số revision tại thời điểm thao tác
        /// </summary>
        public int Revision { get; set; }

        public DateTime AppliedAt { get; set; }
    }
}
