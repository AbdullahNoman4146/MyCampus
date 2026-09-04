using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Services
{
    public class JsonImportService : IJsonImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<JsonImportService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonImportService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<JsonImportService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task SeedAllAsync()
        {
            await ImportHackathonResourcesAsync(clearExisting: false);
        }

        public async Task ImportHackathonResourcesAsync(bool clearExisting = true)
        {
            // Prefer data/ folder as per CampusOS specification, fallback to SeedData/
            var primaryFolder = Path.Combine(_env.ContentRootPath, "data");
            var fallbackFolder = Path.Combine(_env.ContentRootPath, "SeedData", "hackathon");
            var dataFolder = Directory.Exists(primaryFolder) ? primaryFolder : fallbackFolder;

            if (!Directory.Exists(dataFolder))
            {
                _logger.LogWarning("Seed data directory not found at: {Path}", dataFolder);
                return;
            }

            _logger.LogInformation("Importing official seed resources from: {Path}", dataFolder);

            if (clearExisting)
            {
                _logger.LogInformation("Clearing existing SQL Server tables to reload official seed resources...");
                _context.EventRegistrations.RemoveRange(_context.EventRegistrations);
                _context.RoomBookings.RemoveRange(_context.RoomBookings);
                _context.Schedules.RemoveRange(_context.Schedules);
                _context.Rooms.RemoveRange(_context.Rooms);
                _context.Events.RemoveRange(_context.Events);
                _context.Announcements.RemoveRange(_context.Announcements);
                _context.Assignments.RemoveRange(_context.Assignments);
                await _context.SaveChangesAsync();
            }

            // 1. Schedules (data/schedules.json)
            var schedFile = Path.Combine(dataFolder, "schedules.json");
            if (File.Exists(schedFile) && (!await _context.Schedules.AnyAsync() || clearExisting))
            {
                var json = await File.ReadAllTextAsync(schedFile);
                var items = JsonSerializer.Deserialize<List<CampusScheduleDto>>(json, _jsonOptions);
                if (items != null && items.Any())
                {
                    var schedules = items.Select(s => new Schedule
                    {
                        ExternalId = s.Id,
                        Course = s.Course ?? "CSE",
                        Title = s.Title ?? string.Empty,
                        Day = s.Day ?? "Monday",
                        StartTime = s.StartTime ?? "09:00",
                        EndTime = s.EndTime ?? "10:00",
                        Time = $"{s.StartTime} - {s.EndTime}",
                        Room = s.Room ?? "TBA",
                        Instructor = s.Instructor ?? "TBA",
                        Section = s.Section ?? "A"
                    }).ToList();

                    await _context.Schedules.AddRangeAsync(schedules);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Imported {Count} schedules into SQL Server.", schedules.Count);
                }
            }

            // 2. Rooms & Seed Bookings (data/rooms.json)
            var roomsFile = Path.Combine(dataFolder, "rooms.json");
            if (File.Exists(roomsFile) && (!await _context.Rooms.AnyAsync() || clearExisting))
            {
                var json = await File.ReadAllTextAsync(roomsFile);
                var items = JsonSerializer.Deserialize<List<CampusRoomDto>>(json, _jsonOptions);
                if (items != null && items.Any())
                {
                    var seedBookings = new List<RoomBooking>();
                    foreach (var r in items)
                    {
                        var roomEntity = new Room
                        {
                            ExternalId = r.Id,
                            RoomNumber = r.RoomNumber ?? "Room",
                            Type = r.Type ?? "classroom",
                            Capacity = r.Capacity,
                            Equipment = r.Equipment != null && r.Equipment.Any() ? string.Join(", ", r.Equipment) : "whiteboard",
                            Floor = r.Floor > 0 ? r.Floor : 7,
                            BookingStatus = (r.Status?.ToLower() == "available") ? "Available" : "Booked"
                        };
                        _context.Rooms.Add(roomEntity);
                        await _context.SaveChangesAsync();

                        if (r.Bookings != null && r.Bookings.Any())
                        {
                            foreach (var b in r.Bookings)
                            {
                                DateTime bDate;
                                if (!DateTime.TryParse(b.Date, out bDate)) bDate = DateTime.Today;
                                TimeSpan bStart, bEnd;
                                if (!TimeSpan.TryParse(b.StartTime, out bStart)) bStart = new TimeSpan(13, 0, 0);
                                if (!TimeSpan.TryParse(b.EndTime, out bEnd)) bEnd = new TimeSpan(15, 0, 0);

                                seedBookings.Add(new RoomBooking
                                {
                                    ExternalBookingId = b.BookingId,
                                    RoomId = roomEntity.RoomId,
                                    BookedBy = b.BookedBy ?? "Faculty / Staff",
                                    BookingDate = bDate,
                                    StartTime = bStart,
                                    EndTime = bEnd,
                                    Purpose = b.Purpose ?? "Academic Reservation",
                                    Status = "Confirmed",
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    if (seedBookings.Any())
                    {
                        await _context.RoomBookings.AddRangeAsync(seedBookings);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Imported {Count} seed room bookings into SQL Server.", seedBookings.Count);
                    }

                    _logger.LogInformation("Imported {Count} rooms into SQL Server.", items.Count);
                }
            }

            // 3. Events & Registrations (data/events.json)
            var eventsFile = Path.Combine(dataFolder, "events.json");
            if (File.Exists(eventsFile) && (!await _context.Events.AnyAsync() || clearExisting))
            {
                var json = await File.ReadAllTextAsync(eventsFile);
                var items = JsonSerializer.Deserialize<List<CampusEventDto>>(json, _jsonOptions);
                if (items != null && items.Any())
                {
                    foreach (var ev in items)
                    {
                        DateTime parsedDate;
                        if (!DateTime.TryParse(ev.Date, out parsedDate))
                        {
                            parsedDate = DateTime.Today.AddDays(7);
                        }

                        var campusEvent = new CampusEvent
                        {
                            ExternalId = ev.Id,
                            Name = ev.Name ?? "Campus Event",
                            Description = ev.Description,
                            Date = parsedDate,
                            StartTime = ev.StartTime,
                            EndTime = ev.EndTime,
                            Time = $"{ev.StartTime} - {ev.EndTime}",
                            EndDate = ev.EndDate,
                            Venue = ev.Venue ?? "Campus Auditorium",
                            Organizer = ev.Organizer ?? "CSE Department",
                            Capacity = ev.Capacity,
                            RegisteredUsers = ev.Registered,
                            Status = ev.Status ?? "upcoming"
                        };

                        _context.Events.Add(campusEvent);
                        await _context.SaveChangesAsync();

                        // Seed initial attendee registrations from JSON
                        if (ev.Registrations != null && ev.Registrations.Any())
                        {
                            var regEntities = ev.Registrations.Select(r => new EventRegistration
                            {
                                EventId = campusEvent.EventId,
                                StudentId = r.StudentId,
                                StudentName = r.Name ?? "Registered Student",
                                StudentEmail = !string.IsNullOrWhiteSpace(r.StudentId) ? $"{r.StudentId}@aust.edu" : "student@aust.edu",
                                RegistrationDate = DateTime.UtcNow,
                                Status = "Confirmed"
                            }).ToList();

                            await _context.EventRegistrations.AddRangeAsync(regEntities);
                            await _context.SaveChangesAsync();
                        }
                    }
                    _logger.LogInformation("Imported {Count} events with attendees into SQL Server.", items.Count);
                }
            }

            // 4. Announcements (data/announcements.json)
            var annFile = Path.Combine(dataFolder, "announcements.json");
            if (File.Exists(annFile) && (!await _context.Announcements.AnyAsync() || clearExisting))
            {
                var json = await File.ReadAllTextAsync(annFile);
                var items = JsonSerializer.Deserialize<List<CampusAnnouncementDto>>(json, _jsonOptions);
                if (items != null && items.Any())
                {
                    var announcements = items.Select(a =>
                    {
                        DateTime pDate;
                        if (!DateTime.TryParse(a.Date, out pDate)) pDate = DateTime.Today;

                        DateTime? expDate = null;
                        if (!string.IsNullOrWhiteSpace(a.Expires) && DateTime.TryParse(a.Expires, out var parsedExp))
                        {
                            expDate = parsedExp;
                        }

                        var priority = "medium";
                        if (!string.IsNullOrWhiteSpace(a.Priority))
                        {
                            priority = a.Priority.ToLower();
                        }

                        return new Announcement
                        {
                            ExternalId = a.Id,
                            Title = a.Title ?? "Campus Notice",
                            Body = a.Body ?? string.Empty,
                            Date = pDate,
                            Priority = priority,
                            PostedBy = a.PostedBy ?? "Campus Administration",
                            Expires = expDate
                        };
                    }).ToList();

                    await _context.Announcements.AddRangeAsync(announcements);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Imported {Count} announcements into SQL Server.", announcements.Count);
                }
            }

            // 5. Assignments (data/assignments.json)
            var asgnFile = Path.Combine(dataFolder, "assignments.json");
            if (File.Exists(asgnFile) && (!await _context.Assignments.AnyAsync() || clearExisting))
            {
                var json = await File.ReadAllTextAsync(asgnFile);
                var items = JsonSerializer.Deserialize<List<CampusAssignmentDto>>(json, _jsonOptions);
                if (items != null && items.Any())
                {
                    var assignments = items.Select(a =>
                    {
                        DateTime pDeadline;
                        if (!DateTime.TryParse(a.Deadline, out pDeadline)) pDeadline = DateTime.Today.AddDays(5);

                        DateTime? assignedDate = null;
                        if (!string.IsNullOrWhiteSpace(a.AssignedDate) && DateTime.TryParse(a.AssignedDate, out var pAssigned))
                        {
                            assignedDate = pAssigned;
                        }

                        var status = "pending";
                        if (!string.IsNullOrWhiteSpace(a.Status))
                        {
                            status = a.Status.ToLower();
                        }

                        return new Assignment
                        {
                            ExternalId = a.Id,
                            Course = a.Course ?? "CSE",
                            CourseTitle = a.CourseTitle,
                            Title = a.Title ?? "Assignment",
                            Description = a.Description,
                            AssignedDate = assignedDate,
                            Deadline = pDeadline,
                            SubmissionPlatform = a.SubmissionPlatform ?? "Online Submission",
                            Status = status,
                            Marks = a.Marks > 0 ? a.Marks : 10
                        };
                    }).ToList();

                    await _context.Assignments.AddRangeAsync(assignments);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Imported {Count} assignments into SQL Server.", assignments.Count);
                }
            }
        }

        public async Task SeedSchedulesAsync() => await ImportHackathonResourcesAsync(clearExisting: false);
        public async Task SeedRoomsAsync() => await ImportHackathonResourcesAsync(clearExisting: false);
        public async Task SeedEventsAsync() => await ImportHackathonResourcesAsync(clearExisting: false);
        public async Task SeedAnnouncementsAsync() => await ImportHackathonResourcesAsync(clearExisting: false);
        public async Task SeedAssignmentsAsync() => await ImportHackathonResourcesAsync(clearExisting: false);

        // Strongly-typed DTOs matching CampusOS schema specification
        private class CampusScheduleDto
        {
            public string? Id { get; set; }
            public string? Course { get; set; }
            public string? Title { get; set; }
            public string? Day { get; set; }
            [JsonPropertyName("start_time")]
            public string? StartTime { get; set; }
            [JsonPropertyName("end_time")]
            public string? EndTime { get; set; }
            public string? Room { get; set; }
            public string? Instructor { get; set; }
            public string? Section { get; set; }
        }

        private class CampusRoomDto
        {
            public string? Id { get; set; }
            [JsonPropertyName("room_number")]
            public string? RoomNumber { get; set; }
            public string? Type { get; set; }
            public int Capacity { get; set; }
            public List<string>? Equipment { get; set; }
            public int Floor { get; set; }
            public string? Status { get; set; }
            public List<CampusBookingDto>? Bookings { get; set; }
        }

        private class CampusBookingDto
        {
            [JsonPropertyName("booking_id")]
            public string? BookingId { get; set; }
            [JsonPropertyName("booked_by")]
            public string? BookedBy { get; set; }
            public string? Date { get; set; }
            [JsonPropertyName("start_time")]
            public string? StartTime { get; set; }
            [JsonPropertyName("end_time")]
            public string? EndTime { get; set; }
            public string? Purpose { get; set; }
        }

        private class CampusEventDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Date { get; set; }
            [JsonPropertyName("start_time")]
            public string? StartTime { get; set; }
            [JsonPropertyName("end_time")]
            public string? EndTime { get; set; }
            [JsonPropertyName("end_date")]
            public string? EndDate { get; set; }
            public string? Venue { get; set; }
            public string? Organizer { get; set; }
            public int Capacity { get; set; }
            public int Registered { get; set; }
            public List<CampusRegistrationDto>? Registrations { get; set; }
            public string? Status { get; set; }
        }

        private class CampusRegistrationDto
        {
            [JsonPropertyName("student_id")]
            public string? StudentId { get; set; }
            public string? Name { get; set; }
        }

        private class CampusAnnouncementDto
        {
            public string? Id { get; set; }
            public string? Title { get; set; }
            public string? Body { get; set; }
            public string? Date { get; set; }
            public string? Priority { get; set; }
            [JsonPropertyName("posted_by")]
            public string? PostedBy { get; set; }
            public string? Expires { get; set; }
        }

        private class CampusAssignmentDto
        {
            public string? Id { get; set; }
            public string? Course { get; set; }
            [JsonPropertyName("course_title")]
            public string? CourseTitle { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            [JsonPropertyName("assigned_date")]
            public string? AssignedDate { get; set; }
            public string? Deadline { get; set; }
            [JsonPropertyName("submission_platform")]
            public string? SubmissionPlatform { get; set; }
            public string? Status { get; set; }
            public int Marks { get; set; }
        }
    }
}
