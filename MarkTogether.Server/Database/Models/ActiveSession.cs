using System;

namespace MarkTogether.Server.Database.Models
{
    /// <summary>
    /// Model quản lý user đang online trong document.
    /// Không lưu vào database, chỉ dùng in-memory trên Server.
    /// </summary>
    public class ActiveSession
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DocId { get; set; }
        public int CursorPosition { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
