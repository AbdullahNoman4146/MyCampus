using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCampus.Models
{
    public class EventRegistration
    {
        [Key]
        public int RegistrationId { get; set; }

        public string? StudentId { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual CampusEvent? CampusEvent { get; set; }

        [Required(ErrorMessage = "Student name is required.")]
        [StringLength(120)]
        [Display(Name = "Student Name")]
        public string StudentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Student Email")]
        public string StudentEmail { get; set; } = string.Empty;

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled
    }
}
