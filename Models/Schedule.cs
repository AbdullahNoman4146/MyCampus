using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        public string? ExternalId { get; set; }

        public string Course { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string Day { get; set; } = string.Empty;

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        public string Time { get; set; } = string.Empty;

        public string Room { get; set; } = string.Empty;

        public string Instructor { get; set; } = string.Empty;

        public string? Section { get; set; }
    }
}