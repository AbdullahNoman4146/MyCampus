using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedules
        public async Task<IActionResult> Index(string? day, string? search)
        {
            var query = _context.Schedules.AsQueryable();

            if (!string.IsNullOrWhiteSpace(day) && !day.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.Day.ToLower() == day.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s => s.Course.ToLower().Contains(term) || 
                                         (s.Title != null && s.Title.ToLower().Contains(term)) ||
                                         s.Instructor.ToLower().Contains(term) ||
                                         s.Room.ToLower().Contains(term));
            }

            ViewBag.SelectedDay = day;
            ViewBag.SearchTerm = search;

            var list = await query
                .OrderBy(s => s.Day)
                .ThenBy(s => s.StartTime ?? s.Time)
                .ToListAsync();

            return View(list);
        }

        // GET: Schedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(m => m.ScheduleId == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // GET: Schedules/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleId,ExternalId,Course,Title,Time,StartTime,EndTime,Room,Day,Instructor,Section")] Schedule schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule.Time) && !string.IsNullOrWhiteSpace(schedule.StartTime) && !string.IsNullOrWhiteSpace(schedule.EndTime))
            {
                schedule.Time = $"{schedule.StartTime} - {schedule.EndTime}";
            }

            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Class schedule for '{schedule.Course}' was successfully added!";
                return RedirectToAction(nameof(Index));
            }
            return View(schedule);
        }

        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }
            return View(schedule);
        }

        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ScheduleId,ExternalId,Course,Title,Time,StartTime,EndTime,Room,Day,Instructor,Section")] Schedule schedule)
        {
            if (id != schedule.ScheduleId)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(schedule.Time) && !string.IsNullOrWhiteSpace(schedule.StartTime) && !string.IsNullOrWhiteSpace(schedule.EndTime))
            {
                schedule.Time = $"{schedule.StartTime} - {schedule.EndTime}";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Schedule for '{schedule.Course}' was successfully updated!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.ScheduleId))
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
            return View(schedule);
        }

        // GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(m => m.ScheduleId == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? scheduleId = null)
        {
            id = id != 0 ? id : (scheduleId ?? 0);
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                var courseName = schedule.Course;
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Schedule for '{courseName}' was permanently removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.ScheduleId == id);
        }
    }
}
