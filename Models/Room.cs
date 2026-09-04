using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        public string? ExternalId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string? Type { get; set; } = "classroom";

        public int Capacity { get; set; }

        public string Equipment { get; set; } = string.Empty;

        public int Floor { get; set; } = 7;

        public string BookingStatus { get; set; } = "Available";

        public virtual ICollection<RoomBooking> Bookings { get; set; } = new List<RoomBooking>();
    }
}