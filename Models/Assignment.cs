using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }

        public string? ExternalId { get; set; }

        public string Course { get; set; } = string.Empty;

        public string? CourseTitle { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? AssignedDate { get; set; }

        public DateTime Deadline { get; set; }

        public string? SubmissionPlatform { get; set; }

        public string Status { get; set; } = "pending";

        public int Marks { get; set; } = 10;
    }
}