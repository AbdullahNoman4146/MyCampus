using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }

        public string? ExternalId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public string Priority { get; set; } = "medium";

        public string? PostedBy { get; set; }

        public DateTime? Expires { get; set; }
    }
}