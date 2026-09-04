using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Controllers
{
    public class CampusEventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CampusEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CampusEvents
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Events.Include(e => e.Registrations).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(term) ||
                                         (e.Description != null && e.Description.ToLower().Contains(term)) ||
                                         (e.Venue != null && e.Venue.ToLower().Contains(term)) ||
                                         (e.Organizer != null && e.Organizer.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var st = status.Trim().ToLower();
                query = query.Where(e => e.Status != null && e.Status.ToLower() == st);
            }

            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = status;

            var events = await query
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(events);
        }

        // GET: CampusEvents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campusevent = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (campusevent == null)
            {
                return NotFound();
            }

            return View(campusevent);
        }

        // GET: CampusEvents/Register/5
        public async Task<IActionResult> Register(int? id, int? eventId = null)
        {
            id = id ?? eventId;
            if (id == null)
            {
                return NotFound();
            }

            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound();
            }

            var confirmedCount = ev.Registrations.Count(r => r.Status == "Confirmed");
            if (confirmedCount >= ev.Capacity)
            {
                TempData["ErrorMessage"] = "Registration is closed because this event has reached maximum capacity.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            ViewBag.Event = ev;
            var model = new EventRegistration
            {
                EventId = ev.EventId
            };

            return View(model);
        }

        // POST: CampusEvents/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("EventId,StudentId,StudentName,StudentEmail")] EventRegistration registration)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventId == registration.EventId);

            if (ev == null)
            {
                return NotFound();
            }

            // Validation 1: Event Capacity Limit Check
            var confirmedCount = ev.Registrations.Count(r => r.Status == "Confirmed");
            if (confirmedCount >= ev.Capacity)
            {
                ModelState.AddModelError(string.Empty, "Sorry! This event has reached its maximum capacity. No more seats available.");
            }

            // Validation 2: Duplicate Registration Check (by email)
            var isDuplicate = ev.Registrations.Any(r =>
                r.Status == "Confirmed" &&
                r.StudentEmail.Trim().Equals(registration.StudentEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                ModelState.AddModelError("StudentEmail", "You have already registered for this event with this email address.");
            }

            if (ModelState.IsValid)
            {
                registration.RegistrationDate = DateTime.UtcNow;
                registration.Status = "Confirmed";

                _context.EventRegistrations.Add(registration);

                // Increment Registered Users count permanently in SQL Server
                ev.RegisteredUsers = confirmedCount + 1;
                _context.Events.Update(ev);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Congratulations {registration.StudentName}! You are officially registered for '{ev.Name}'.";
                return RedirectToAction(nameof(Details), new { id = ev.EventId });
            }

            ViewBag.Event = ev;
            return View(registration);
        }

        // POST: CampusEvents/CancelRegistration/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int registrationId)
        {
            var reg = await _context.EventRegistrations
                .Include(r => r.CampusEvent)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (reg == null)
            {
                return NotFound();
            }

            var eventId = reg.EventId;
            reg.Status = "Cancelled";
            _context.EventRegistrations.Update(reg);

            if (reg.CampusEvent != null && reg.CampusEvent.RegisteredUsers > 0)
            {
                reg.CampusEvent.RegisteredUsers--;
                _context.Events.Update(reg.CampusEvent);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your registration has been cancelled.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        // GET: CampusEvents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CampusEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,ExternalId,Name,Description,Date,Time,StartTime,EndTime,EndDate,Venue,Organizer,Capacity,Status")] CampusEvent campusevent)
        {
            if (string.IsNullOrWhiteSpace(campusevent.Time) && !string.IsNullOrWhiteSpace(campusevent.StartTime) && !string.IsNullOrWhiteSpace(campusevent.EndTime))
            {
                campusevent.Time = $"{campusevent.StartTime} - {campusevent.EndTime}";
            }

            if (string.IsNullOrWhiteSpace(campusevent.Status)) campusevent.Status = "upcoming";

            if (ModelState.IsValid)
            {
                campusevent.RegisteredUsers = 0;
                _context.Add(campusevent);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{campusevent.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(campusevent);
        }

        // GET: CampusEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campusevent = await _context.Events.FindAsync(id);
            if (campusevent == null)
            {
                return NotFound();
            }
            return View(campusevent);
        }

        // POST: CampusEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,ExternalId,Name,Description,Date,Time,StartTime,EndTime,EndDate,Venue,Organizer,Capacity,RegisteredUsers,Status")] CampusEvent campusevent)
        {
            if (id != campusevent.EventId)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(campusevent.Time) && !string.IsNullOrWhiteSpace(campusevent.StartTime) && !string.IsNullOrWhiteSpace(campusevent.EndTime))
            {
                campusevent.Time = $"{campusevent.StartTime} - {campusevent.EndTime}";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(campusevent);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Event '{campusevent.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CampusEventExists(campusevent.EventId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(campusevent);
        }

        // GET: CampusEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campusevent = await _context.Events
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (campusevent == null)
            {
                return NotFound();
            }

            return View(campusevent);
        }

        // POST: CampusEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? eventId = null)
        {
            id = id != 0 ? id : (eventId ?? 0);
            var campusevent = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (campusevent != null)
            {
                var eventName = campusevent.Name;
                _context.EventRegistrations.RemoveRange(campusevent.Registrations);
                _context.Events.Remove(campusevent);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{eventName}' was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CampusEventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}
