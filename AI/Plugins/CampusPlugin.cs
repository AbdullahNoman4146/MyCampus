using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.AI.Plugins
{
    public class CampusPlugin
    {
        private readonly ApplicationDbContext _context;

        public CampusPlugin(ApplicationDbContext context)
        {
            _context = context;
        }

        [KernelFunction, Description("Retrieves class schedule information from SQL Server. Can filter by course name, day of the week, instructor name, or find the next upcoming class. Cross-references live announcements for room relocations or cancellations.")]
        public async Task<string> GetSchedule(
            [Description("Course name or code, e.g. 'CSE 4113'")] string? course = null,
            [Description("Day of the week, e.g. 'Monday', 'Sunday'")] string? day = null,
            [Description("Instructor name, e.g. 'Prof. Dr. Faisal Muhammad Shah'")] string? instructor = null,
            [Description("Set to true if user specifically asks for their next upcoming class")] bool nextClassOnly = false)
        {
            // First check if any announcement overrides this course's schedule or room
            string? announcementOverride = null;
            if (!string.IsNullOrWhiteSpace(course))
            {
                var cleanCourse = course.Trim().ToUpper();
                var matchingAnn = await _context.Announcements
                    .Where(a => a.Title.Contains(cleanCourse) || a.Body.Contains(cleanCourse))
                    .OrderByDescending(a => a.Date)
                    .FirstOrDefaultAsync();

                if (matchingAnn != null)
                {
                    announcementOverride = $"> 📢 **Important Update from Announcements:**\n> **{matchingAnn.Title}**\n> *\"{matchingAnn.Body}\"*\n";
                }
            }

            if (nextClassOnly)
            {
                return await GetNextUpcomingClassAsync(announcementOverride);
            }

            var query = _context.Schedules.AsQueryable();

            if (!string.IsNullOrWhiteSpace(course))
            {
                query = query.Where(s => s.Course.Contains(course) || (s.Title != null && s.Title.Contains(course)));
            }

            if (!string.IsNullOrWhiteSpace(day))
            {
                query = query.Where(s => s.Day.ToLower() == day.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(instructor))
            {
                query = query.Where(s => s.Instructor.Contains(instructor));
            }

            var results = await query.ToListAsync();
            if (!results.Any())
            {
                if (!string.IsNullOrEmpty(announcementOverride))
                {
                    return $"{announcementOverride}\n(No recurring schedule found matching '{course}', but please see the announcement above.)";
                }
                return "No classes found matching your criteria in the SQL Server schedule table.";
            }

            var output = "### 📖 University Schedule (Live SQL Server Data):\n";
            if (!string.IsNullOrEmpty(announcementOverride))
            {
                output += $"{announcementOverride}\n";
            }

            foreach (var item in results)
            {
                var titleText = !string.IsNullOrEmpty(item.Title) ? $" - {item.Title}" : "";
                var secText = !string.IsNullOrEmpty(item.Section) ? $" [Sec {item.Section}]" : "";
                var timeText = !string.IsNullOrEmpty(item.StartTime) ? $"{item.StartTime} - {item.EndTime}" : item.Time;
                output += $"- **{item.Course}{titleText}{secText}** | ⏰ {timeText} | 🚪 {item.Room} | 🗓️ {item.Day} | 👨‍🏫 {item.Instructor}\n";
            }

            return output;
        }

        private async Task<string> GetNextUpcomingClassAsync(string? announcementOverride)
        {
            var today = DateTime.Today.DayOfWeek.ToString(); // e.g. Sunday
            var daysOfWeekOrder = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday" };

            // Find current or nearest day with classes
            var allSchedules = await _context.Schedules.ToListAsync();
            if (!allSchedules.Any())
            {
                return "No schedule records found in SQL Server.";
            }

            Schedule? nextClass = null;

            // Check today first
            var todayClasses = allSchedules.Where(s => s.Day.Equals(today, StringComparison.OrdinalIgnoreCase)).ToList();
            var nowTime = DateTime.Now.ToString("HH:mm");
            
            nextClass = todayClasses
                .Where(s => string.Compare(s.StartTime ?? s.Time, nowTime, StringComparison.Ordinal) >= 0)
                .OrderBy(s => s.StartTime ?? s.Time)
                .FirstOrDefault();

            // If no more classes today, find the earliest class on the next scheduled day in weekly sequence
            if (nextClass == null)
            {
                int todayIndex = Array.IndexOf(daysOfWeekOrder, today);
                if (todayIndex < 0) todayIndex = 0; // If Friday or Saturday, next academic day is Sunday

                for (int i = 1; i <= 5; i++)
                {
                    var targetDay = daysOfWeekOrder[(todayIndex + i) % daysOfWeekOrder.Length];
                    var nextDayClasses = allSchedules
                        .Where(s => s.Day.Equals(targetDay, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.StartTime ?? s.Time)
                        .ToList();

                    if (nextDayClasses.Any())
                    {
                        nextClass = nextDayClasses.First();
                        break;
                    }
                }
            }

            // Fallback to first class in list if cycle did not resolve
            if (nextClass == null)
            {
                nextClass = allSchedules.OrderBy(s => s.Day).ThenBy(s => s.StartTime).First();
            }

            // Check announcement override specifically for this next class
            if (string.IsNullOrEmpty(announcementOverride) && !string.IsNullOrEmpty(nextClass.Course))
            {
                var matchingAnn = await _context.Announcements
                    .Where(a => a.Title.Contains(nextClass.Course) || a.Body.Contains(nextClass.Course))
                    .OrderByDescending(a => a.Date)
                    .FirstOrDefaultAsync();

                if (matchingAnn != null)
                {
                    announcementOverride = $"> ⚠️ **Schedule Alert:** {matchingAnn.Title}\n> *{matchingAnn.Body}*\n";
                }
            }

            var titleStr = !string.IsNullOrEmpty(nextClass.Title) ? $" — {nextClass.Title}" : "";
            var secStr = !string.IsNullOrEmpty(nextClass.Section) ? $" (Section {nextClass.Section})" : "";
            var timeStr = !string.IsNullOrEmpty(nextClass.StartTime) ? $"{nextClass.StartTime} to {nextClass.EndTime}" : nextClass.Time;

            var result = $"### 🔔 Your Next Class:\n" +
                         $"- **Course:** {nextClass.Course}{titleStr}{secStr}\n" +
                         $"- **Day & Time:** {nextClass.Day} at {timeStr}\n" +
                         $"- **Room:** {nextClass.Room}\n" +
                         $"- **Instructor:** {nextClass.Instructor}\n";

            if (!string.IsNullOrEmpty(announcementOverride))
            {
                result = $"{announcementOverride}\n{result}";
            }

            return result;
        }

        [KernelFunction, Description("Finds academic assignments from SQL Server. Can filter by course name, status ('pending' or 'submitted'), or restrict to assignments due within the upcoming week.")]
        public async Task<string> FindAssignment(
            [Description("Course name or code, e.g. 'CSE 4113'")] string? course = null,
            [Description("Assignment status, e.g. 'pending' or 'submitted'")] string? status = null,
            [Description("If true, only returns assignments due within the current week")] bool dueThisWeek = false)
        {
            var query = _context.Assignments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(course))
            {
                query = query.Where(a => a.Course.Contains(course) || (a.CourseTitle != null && a.CourseTitle.Contains(course)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToLower();
                query = query.Where(a => a.Status.ToLower() == st);
            }

            if (dueThisWeek)
            {
                var weekEnd = DateTime.Today.AddDays(7);
                query = query.Where(a => a.Deadline <= weekEnd && a.Status.ToLower() == "pending");
            }

            var results = await query.OrderBy(a => a.Deadline).ToListAsync();
            if (!results.Any())
            {
                return "No assignments found matching the criteria in SQL Server.";
            }

            var title = dueThisWeek ? "Academic Assignments Due This Week (Live SQL Server Data)" : "Academic Assignments (Live SQL Server Data)";
            var output = $"### 📝 {title}:\n";
            foreach (var item in results)
            {
                var daysLeft = (item.Deadline.Date - DateTime.Today).TotalDays;
                var dueText = daysLeft < 0 ? "⚠️ Overdue" : (daysLeft == 0 ? "⚠️ Due Today!" : $"Due in {daysLeft} day{(daysLeft == 1 ? "" : "s")}");
                var courseInfo = !string.IsNullOrEmpty(item.CourseTitle) ? $"{item.Course} ({item.CourseTitle})" : item.Course;
                output += $"- **{courseInfo}**: {item.Title}\n  - **Marks:** {item.Marks} | **Platform:** {item.SubmissionPlatform}\n  - **Deadline:** {item.Deadline:MMM dd, yyyy} ({dueText})\n  - **Status:** `{item.Status}`\n";
            }

            return output;
        }

        [KernelFunction, Description("Searches campus rooms and facilities in SQL Server by minimum capacity, equipment keywords, room type, availability, and checks conflict-free availability on a specific date and time slot.")]
        public async Task<string> SearchRoom(
            [Description("Minimum required capacity, e.g. 30, 40, or 5")] int? minCapacity = null,
            [Description("Required equipment, e.g. 'projector', 'smart board', 'computers', 'AC'")] string? equipment = null,
            [Description("Room type: 'classroom', 'lab', or 'seminar'")] string? roomType = null,
            [Description("Availability status, e.g. 'Available' or 'Booked'")] string? availability = null,
            [Description("Optional booking date to check conflicts, YYYY-MM-DD")] string? date = null,
            [Description("Optional start time, e.g. '14:00'")] string? startTime = null,
            [Description("Optional end time, e.g. '16:00'")] string? endTime = null)
        {
            var query = _context.Rooms.AsQueryable();

            if (minCapacity.HasValue && minCapacity.Value > 0)
            {
                query = query.Where(r => r.Capacity >= minCapacity.Value);
            }

            if (!string.IsNullOrWhiteSpace(equipment))
            {
                var eq = equipment.Trim().ToLower();
                query = query.Where(r => r.Equipment != null && r.Equipment.ToLower().Contains(eq));
            }

            if (!string.IsNullOrWhiteSpace(roomType))
            {
                var rt = roomType.Trim().ToLower();
                query = query.Where(r => r.Type != null && r.Type.ToLower() == rt);
            }

            if (!string.IsNullOrWhiteSpace(availability) && !availability.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var av = availability.Trim().ToLower();
                query = query.Where(r => r.BookingStatus != null && r.BookingStatus.ToLower() == av);
            }

            // If checking a specific time window, exclude rooms with conflicting bookings
            if (!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(startTime) && !string.IsNullOrWhiteSpace(endTime))
            {
                if (DateTime.TryParse(date, out var checkDate) && TimeSpan.TryParse(startTime, out var startT) && TimeSpan.TryParse(endTime, out var endT))
                {
                    var bookedRoomIds = await _context.RoomBookings
                        .Where(b => b.BookingDate.Date == checkDate.Date && b.Status == "Confirmed" && startT < b.EndTime && endT > b.StartTime)
                        .Select(b => b.RoomId)
                        .ToListAsync();

                    if (bookedRoomIds.Any())
                    {
                        query = query.Where(r => !bookedRoomIds.Contains(r.RoomId));
                    }
                }
            }

            var results = await query.OrderBy(r => r.RoomNumber).ToListAsync();
            if (!results.Any())
            {
                return "No rooms matching your search parameters were found in SQL Server.";
            }

            var timeSlotNotice = (!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(startTime)) 
                ? $" (Verified Available on {date} from {startTime} to {endTime})" 
                : "";

            var output = $"### 🚪 Campus Rooms Found ({results.Count}){timeSlotNotice}:\n";
            foreach (var item in results)
            {
                var typeLabel = item.Type?.ToUpper() ?? "ROOM";
                output += $"- **{item.RoomNumber}** [{typeLabel}] (Floor {item.Floor}, Capacity: {item.Capacity} seats, Status: `{item.BookingStatus}`)\n  - *Equipment:* {item.Equipment}\n";
            }

            return output;
        }

        [KernelFunction, Description("Books a campus room in SQL Server. Checks room existence, verifies availability, and prevents double-booking.")]
        public async Task<string> BookRoom(
            [Description("Room number, e.g. '7A01', '7A02', '7B04', or '7C02'")] string roomNumber,
            [Description("Date of booking in format YYYY-MM-DD, e.g. '2026-09-18'")] string date,
            [Description("Start time, e.g. '15:00' or '10:00'")] string startTime,
            [Description("End time, e.g. '17:00' or '12:00'")] string endTime,
            [Description("Purpose of booking")] string purpose,
            [Description("Name or email of the person booking")] string bookedBy)
        {
            var trimmed = roomNumber.Trim();
            var cleanRoomNumber = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^Room\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            var room = await _context.Rooms.FirstOrDefaultAsync(r => 
                r.RoomNumber.ToLower() == trimmed.ToLower() || 
                r.RoomNumber.ToLower() == cleanRoomNumber.ToLower());

            if (room == null)
            {
                room = await _context.Rooms.FirstOrDefaultAsync(r => 
                    r.RoomNumber.Contains(trimmed) || 
                    r.RoomNumber.Contains(cleanRoomNumber) ||
                    cleanRoomNumber.Contains(r.RoomNumber));

                if (room == null)
                {
                    return $"❌ Error: Room '{roomNumber}' could not be found in the database.";
                }
            }

            if (!DateTime.TryParse(date, out var bookingDate))
            {
                bookingDate = DateTime.Today.AddDays(1);
            }

            if (bookingDate.Date < DateTime.Today)
            {
                return "❌ Error: Cannot book a room for a past date.";
            }

            if (!TimeSpan.TryParse(startTime, out var parsedStart))
            {
                parsedStart = new TimeSpan(15, 0, 0);
            }

            if (!TimeSpan.TryParse(endTime, out var parsedEnd))
            {
                parsedEnd = parsedStart.Add(TimeSpan.FromHours(2));
            }

            if (parsedEnd <= parsedStart)
            {
                return "❌ Error: End time must be after start time.";
            }

            // Conflict Prevention Check in SQL Server
            var hasConflict = await _context.RoomBookings.AnyAsync(b =>
                b.RoomId == room.RoomId &&
                b.BookingDate.Date == bookingDate.Date &&
                b.Status == "Confirmed" &&
                parsedStart < b.EndTime &&
                parsedEnd > b.StartTime);

            if (hasConflict)
            {
                return $"❌ Conflict Error: {room.RoomNumber} is already booked on {bookingDate:MMM dd, yyyy} between {parsedStart:hh\\:mm} and {parsedEnd:hh\\:mm}. Please select a different time slot or another room.";
            }

            var newBooking = new RoomBooking
            {
                RoomId = room.RoomId,
                BookingDate = bookingDate.Date,
                StartTime = parsedStart,
                EndTime = parsedEnd,
                Purpose = string.IsNullOrWhiteSpace(purpose) ? "Campus Activity" : purpose,
                BookedBy = string.IsNullOrWhiteSpace(bookedBy) ? "Student / Faculty" : bookedBy,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            _context.RoomBookings.Add(newBooking);
            await _context.SaveChangesAsync();

            return $"✅ Success: Room **{room.RoomNumber}** is successfully booked for **{bookingDate:MMM dd, yyyy}** from **{parsedStart:hh\\:mm}** to **{parsedEnd:hh\\:mm}**.\n- **Booking ID:** #{newBooking.BookingId}\n- **Booked By:** {newBooking.BookedBy}\n- **Purpose:** {newBooking.Purpose}\n*(Record permanently saved to SQL Server)*";
        }

        [KernelFunction, Description("Retrieves active campus announcements and notices from SQL Server. Can filter by priority ('high', 'medium', 'low') or search keywords.")]
        public async Task<string> GetAnnouncements(
            [Description("Priority filter: 'high', 'urgent', 'medium', or 'low'")] string? priority = null,
            [Description("Search term or course code to filter notices")] string? searchTerm = null)
        {
            var query = _context.Announcements.AsQueryable();

            if (!string.IsNullOrWhiteSpace(priority))
            {
                var pri = priority.Trim().ToLower();
                if (pri == "urgent") pri = "high";
                query = query.Where(a => a.Priority.ToLower() == pri);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(term) || a.Body.ToLower().Contains(term));
            }

            var results = await query.OrderByDescending(a => a.Date).ToListAsync();
            if (!results.Any())
            {
                return "No announcements found matching your criteria in SQL Server.";
            }

            var output = $"### 📢 Campus Announcements ({results.Count} notices):\n";
            foreach (var a in results)
            {
                var priBadge = a.Priority.ToLower() == "high" ? "🔴 HIGH" : (a.Priority.ToLower() == "medium" ? "🟡 MEDIUM" : "🟢 LOW");
                var postedText = !string.IsNullOrEmpty(a.PostedBy) ? $" | 👤 {a.PostedBy}" : "";
                output += $"- **{a.Title}** [{priBadge}{postedText}]\n  - *Date:* {a.Date:MMM dd, yyyy}\n  - *Details:* {a.Body}\n\n";
            }

            return output.TrimEnd();
        }

        [KernelFunction, Description("Retrieves campus events from SQL Server. Can search by event title/topic or date.")]
        public async Task<string> GetCampusEvents(
            [Description("Search keyword or topic, e.g. 'hackathon', 'lecture', 'deep learning'")] string? searchTerm = null,
            [Description("Date in YYYY-MM-DD format")] string? date = null)
        {
            var query = _context.Events.Include(e => e.Registrations).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(term) || (e.Description != null && e.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var filterDate))
            {
                query = query.Where(e => e.Date.Date == filterDate.Date);
            }

            var results = await query.OrderBy(e => e.Date).ToListAsync();
            if (!results.Any())
            {
                return "No upcoming campus events matching your criteria were found in SQL Server.";
            }

            var output = $"### 🎯 Campus Events ({results.Count}):\n";
            foreach (var ev in results)
            {
                var confirmedCount = ev.Registrations.Count(r => r.Status == "Confirmed");
                var seatsLeft = ev.Capacity - confirmedCount;
                var venue = !string.IsNullOrEmpty(ev.Venue) ? $"Room {ev.Venue}" : "Campus Hall";
                var time = !string.IsNullOrEmpty(ev.Time) ? ev.Time : (!string.IsNullOrEmpty(ev.StartTime) ? $"{ev.StartTime} - {ev.EndTime}" : "TBA");
                output += $"- **{ev.Name}**\n  - 🗓️ {ev.Date:MMM dd, yyyy} at {time} | 📍 {venue}\n  - 👥 Seats: {confirmedCount}/{ev.Capacity} ({seatsLeft} seats remaining)\n  - ℹ️ {ev.Description}\n\n";
            }

            return output.TrimEnd();
        }

        [KernelFunction, Description("Registers an attendee for a university event in SQL Server. Checks event capacity limits and prevents duplicate registrations.")]
        public async Task<string> RegisterEvent(
            [Description("Name of the event, e.g. 'Guest Lecture: Deep Learning in Medical Imaging'")] string eventName,
            [Description("Student's full name")] string studentName,
            [Description("Student's email address")] string studentEmail,
            [Description("Optional student ID, e.g. '20-40532'")] string? studentId = null)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Name.ToLower() == eventName.Trim().ToLower());

            if (ev == null)
            {
                ev = await _context.Events
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Name.Contains(eventName.Trim()));

                if (ev == null)
                {
                    return $"❌ Error: Event matching '{eventName}' was not found in SQL Server.";
                }
            }

            // Check capacity limit
            var confirmedCount = ev.Registrations.Count(r => r.Status == "Confirmed");
            if (confirmedCount >= ev.Capacity)
            {
                return $"❌ Capacity Reached: '{ev.Name}' has reached its maximum capacity of {ev.Capacity} attendees. Registration is closed.";
            }

            // Check duplicate registration
            var isDuplicate = ev.Registrations.Any(r =>
                r.Status == "Confirmed" &&
                r.StudentEmail.Trim().Equals(studentEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                return $"❌ Duplicate Registration: '{studentEmail}' is already registered for '{ev.Name}'.";
            }

            var reg = new EventRegistration
            {
                EventId = ev.EventId,
                StudentId = studentId,
                StudentName = studentName,
                StudentEmail = studentEmail,
                RegistrationDate = DateTime.UtcNow,
                Status = "Confirmed"
            };

            _context.EventRegistrations.Add(reg);
            ev.RegisteredUsers = confirmedCount + 1;
            _context.Events.Update(ev);

            await _context.SaveChangesAsync();

            var venueStr = !string.IsNullOrEmpty(ev.Venue) ? ev.Venue : "Campus";
            var timeStr = !string.IsNullOrEmpty(ev.Time) ? ev.Time : (!string.IsNullOrEmpty(ev.StartTime) ? $"{ev.StartTime} - {ev.EndTime}" : "Scheduled time");

            return $"✅ Success: **{studentName}** ({studentEmail}) has been successfully registered for **{ev.Name}**!\n- **Registration ID:** #{reg.RegistrationId}\n- **Venue:** {venueStr}\n- **Event Date:** {ev.Date:MMM dd, yyyy} at {timeStr}\n- **Seats Remaining:** {ev.Capacity - ev.RegisteredUsers}\n*(Record permanently saved to SQL Server)*";
        }

        [KernelFunction, Description("Provides multi-source recommendations for students with free time before a specified cutoff hour (e.g. 2 PM). Correlates event schedules, class routines, and available open rooms/study labs.")]
        public async Task<string> GetFreeTimeRecommendations(
            [Description("Cutoff time in 24h format, e.g. '14:00'")] string? freeUntilTime = "14:00",
            [Description("Date in format YYYY-MM-DD")] string? date = null)
        {
            var cutoff = TimeSpan.TryParse(freeUntilTime, out var ts) ? ts : new TimeSpan(14, 0, 0);

            // 1. Events running or starting before cutoff
            var events = await _context.Events
                .Where(e => e.Status == "upcoming")
                .ToListAsync();

            var dropInEvents = events.Where(e =>
            {
                if (TimeSpan.TryParse(e.StartTime, out var evStart))
                {
                    return evStart < cutoff;
                }
                return true;
            }).Take(3).ToList();

            // 2. Open study rooms or labs with capacity
            var openRooms = await _context.Rooms
                .Where(r => r.BookingStatus == "Available" && r.Capacity >= 20)
                .Take(4)
                .ToListAsync();

            var output = $"### 🎯 Campus Activities & Drop-in Options (Free until {cutoff:hh\\:mm}):\n\n";

            if (dropInEvents.Any())
            {
                output += "**🎪 Events You Can Drop Into:**\n";
                foreach (var ev in dropInEvents)
                {
                    var timeStr = !string.IsNullOrEmpty(ev.StartTime) ? $"{ev.StartTime} - {ev.EndTime}" : ev.Time;
                    output += $"- **{ev.Name}** (📍 Room {ev.Venue}, ⏰ {timeStr})\n  - *Details:* {ev.Description}\n";
                }
                output += "\n";
            }

            if (openRooms.Any())
            {
                output += "**🚪 Open Rooms & Study Spaces Available:**\n";
                foreach (var r in openRooms)
                {
                    var typeLabel = r.Type?.ToUpper() ?? "ROOM";
                    output += $"- **{r.RoomNumber}** [{typeLabel}] (Floor {r.Floor}, Capacity: {r.Capacity} seats, Equipment: {r.Equipment})\n";
                }
                output += "\n";
            }

            output += "💡 *Tip: You can ask me to book any of these study spaces or register you for the events above!*";
            return output;
        }

        [KernelFunction, Description("Cancels an existing room booking in SQL Server.")]
        public async Task<string> CancelRoomBooking(
            [Description("Booking ID or Room number")] string bookingRef,
            [Description("Name or email of the person who booked")] string? bookedBy = null)
        {
            RoomBooking? booking = null;
            if (int.TryParse(bookingRef, out var bId))
            {
                booking = await _context.RoomBookings.FindAsync(bId);
            }

            if (booking == null)
            {
                booking = await _context.RoomBookings
                    .Include(b => b.Room)
                    .Where(b => (b.Room != null && b.Room.RoomNumber == bookingRef) || b.ExternalBookingId == bookingRef)
                    .OrderByDescending(b => b.BookingDate)
                    .FirstOrDefaultAsync();
            }

            if (booking == null)
            {
                return $"❌ Error: No active booking matching '{bookingRef}' was found in SQL Server.";
            }

            booking.Status = "Cancelled";
            _context.RoomBookings.Update(booking);
            await _context.SaveChangesAsync();

            return $"✅ Success: Room booking #{booking.BookingId} has been cancelled in SQL Server.";
        }

        [KernelFunction, Description("Cancels a student registration for a campus event in SQL Server.")]
        public async Task<string> CancelEventRegistration(
            [Description("Event name")] string eventName,
            [Description("Student email")] string studentEmail)
        {
            var reg = await _context.EventRegistrations
                .Include(r => r.CampusEvent)
                .FirstOrDefaultAsync(r => r.StudentEmail.ToLower() == studentEmail.Trim().ToLower() && r.CampusEvent != null && r.CampusEvent.Name.Contains(eventName.Trim()));

            if (reg == null)
            {
                return $"❌ Error: No registration found for '{studentEmail}' in '{eventName}'.";
            }

            reg.Status = "Cancelled";
            _context.EventRegistrations.Update(reg);
            if (reg.CampusEvent != null && reg.CampusEvent.RegisteredUsers > 0)
            {
                reg.CampusEvent.RegisteredUsers--;
                _context.Events.Update(reg.CampusEvent);
            }

            await _context.SaveChangesAsync();
            return $"✅ Success: Registration #{reg.RegistrationId} for '{reg.CampusEvent?.Name}' has been cancelled.";
        }
    }
}
