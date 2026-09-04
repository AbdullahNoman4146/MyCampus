using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class CampusEvent
    {

        [Key]
        public int EventId { get; set; }


        public string Name { get; set; }


        public DateTime Date { get; set; }


        public string Time { get; set; }


        public int Capacity { get; set; }


    }
}