using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class CampusEvent
    {
        [Key]
        public int EventId { get; set; }

        public string? ExternalId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime Date { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        public string Time { get; set; } = string.Empty;

        public string? EndDate { get; set; }

        public string? Venue { get; set; }

        public string? Organizer { get; set; }

        public int Capacity { get; set; }

        public int RegisteredUsers { get; set; } = 0;

        public string Status { get; set; } = "upcoming";

        public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    }
}