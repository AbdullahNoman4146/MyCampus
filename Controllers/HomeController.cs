using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;
using MyCampus.ViewModels;
using System.Diagnostics;

namespace MyCampus.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var todayDayName = DateTime.Now.DayOfWeek.ToString();

            // Section 1: Today's schedule (or active schedules)
            var todaySchedules = await _context.Schedules
                .Where(s => s.Day.ToLower() == todayDayName.ToLower())
                .ToListAsync();

            if (!todaySchedules.Any())
            {
                todaySchedules = await _context.Schedules
                    .Take(6)
                    .ToListAsync();
            }

            var vm = new DashboardViewModel
            {
                // KPI Cards
                TotalClassesCount = await _context.Schedules.CountAsync(),
                AvailableRoomsCount = await _context.Rooms.CountAsync(r => r.BookingStatus == "Available"),
                UpcomingEventsCount = await _context.Events.CountAsync(e => e.Date >= DateTime.Today.AddDays(-1)),
                PendingAssignmentsCount = await _context.Assignments.CountAsync(a => a.Status == "Pending"),
                RecentAnnouncementsCount = await _context.Announcements.CountAsync(),

                // 5 Sections
                TodaySchedules = todaySchedules,
                AvailableRooms = await _context.Rooms.Where(r => r.BookingStatus == "Available").Take(6).ToListAsync(),
                UpcomingEvents = await _context.Events.OrderBy(e => e.Date).Take(4).ToListAsync(),
                LatestAnnouncements = await _context.Announcements.OrderByDescending(a => a.Date).Take(4).ToListAsync(),
                AssignmentDeadlines = await _context.Assignments.OrderBy(a => a.Deadline).Take(5).ToListAsync()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
