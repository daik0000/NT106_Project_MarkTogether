using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// POCO đại diện cho bảng "images" trong PostgreSQL.
    /// Lưu trữ ảnh đính kèm tài liệu.
    /// </summary>
    public class Image
    {
        public string Id { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; }

        /// <summary>
        /// Dữ liệu binary của ảnh (BYTEA trong PostgreSQL)
        /// </summary>
        public byte[] FileData { get; set; }

        /// <summary>
        /// Đường dẫn URL hoặc file path trên server
        /// </summary>
        public string Url { get; set; }

        public string MimeType { get; set; }
        public int? Size { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
