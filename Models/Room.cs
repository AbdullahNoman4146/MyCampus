using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Room
    {

        [Key]
        public int RoomId { get; set; }


        public string RoomNumber { get; set; }


        public int Capacity { get; set; }


        public string Equipment { get; set; }


        public string BookingStatus { get; set; }

    }
}