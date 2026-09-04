using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }


        public string Course { get; set; }


        public string Time { get; set; }


        public string Room { get; set; }


        public string Day { get; set; }


        public string Instructor { get; set; }
    }
}