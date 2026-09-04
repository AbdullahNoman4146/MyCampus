using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Assignment
    {

        [Key]
        public int AssignmentId { get; set; }


        public string Course { get; set; }


        public string Title { get; set; }


        public DateTime Deadline { get; set; }


        public string Status { get; set; }

    }
}