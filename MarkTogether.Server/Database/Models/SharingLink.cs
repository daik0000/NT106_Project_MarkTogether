using System;

namespace MarkTogether.Server.Database.Models
{
    public class SharingLink
    {
        public string Id { get; set; }
        public string DocId { get; set; }
        public int CreatedBy { get; set; }
        public string Token { get; set; }
        public string Permission { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUses { get; set; }
        public int UseCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}