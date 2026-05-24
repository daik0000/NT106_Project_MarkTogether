using System;

namespace MarkTogether.Server.Database.Models
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public string DocId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}