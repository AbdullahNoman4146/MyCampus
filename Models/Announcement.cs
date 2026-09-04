using System.ComponentModel.DataAnnotations;

namespace MyCampus.Models
{
    public class Announcement
    {

        [Key]
        public int AnnouncementId { get; set; }


        public string Title { get; set; }


        public string Body { get; set; }


        public DateTime Date { get; set; }


        public string Priority { get; set; }

    }
}