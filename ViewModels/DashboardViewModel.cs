using MyCampus.Models;

namespace MyCampus.ViewModels
{
    public class DashboardViewModel
    {
        // KPI Summary Metric Cards
        public int TotalClassesCount { get; set; }
        public int AvailableRoomsCount { get; set; }
        public int UpcomingEventsCount { get; set; }
        public int PendingAssignmentsCount { get; set; }
        public int RecentAnnouncementsCount { get; set; }

        // Section 1: Today's Schedule
        public List<Schedule> TodaySchedules { get; set; } = new List<Schedule>();

        // Section 2: Available Rooms
        public List<Room> AvailableRooms { get; set; } = new List<Room>();

        // Section 3: Upcoming Events
        public List<CampusEvent> UpcomingEvents { get; set; } = new List<CampusEvent>();

        // Section 4: Latest Announcements
        public List<Announcement> LatestAnnouncements { get; set; } = new List<Announcement>();

        // Section 5: Assignment Deadlines
        public List<Assignment> AssignmentDeadlines { get; set; } = new List<Assignment>();
    }
}
