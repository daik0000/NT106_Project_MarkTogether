using System;

namespace MarkTogether.Server.Database.Models
{
    public class Comment
    {
        public string Id { get; set; }
        public string DocId { get; set; }
        public int UserId { get; set; }
        public string ParentId { get; set; }
        public int AnchorStart { get; set; }
        public int AnchorEnd { get; set; }
        public string AnchorText { get; set; }
        public string Content { get; set; }
        public bool Resolved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}