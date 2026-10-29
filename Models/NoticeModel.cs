using System;

namespace SmartOfficeSystem.Models
{
    public class NoticeModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; } = DateTime.Now;
        public string? PostedBy { get; set; }
    }
}