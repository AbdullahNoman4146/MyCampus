using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCampus.Models
{
    public class RoomBooking
    {
        [Key]
        public int BookingId { get; set; }

        public string? ExternalBookingId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        [Required(ErrorMessage = "Please specify who is booking this room.")]
        [StringLength(150)]
        [Display(Name = "Booked By")]
        public string BookedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a booking date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Please specify start time.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Please specify end time.")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Please specify the purpose for booking.")]
        [StringLength(250)]
        public string Purpose { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
