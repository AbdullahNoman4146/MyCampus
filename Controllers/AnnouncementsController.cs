using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Announcements
        public async Task<IActionResult> Index(string? priority, string? search)
        {
            var query = _context.Announcements.AsQueryable();

            if (!string.IsNullOrWhiteSpace(priority) && !priority.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var pri = priority.Trim().ToLower();
                if (pri == "urgent") pri = "high";
                query = query.Where(a => a.Priority.ToLower() == pri);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(term) ||
                                         a.Body.ToLower().Contains(term) ||
                                         (a.PostedBy != null && a.PostedBy.ToLower().Contains(term)));
            }

            ViewBag.SelectedPriority = priority;
            ViewBag.SearchTerm = search;

            var list = await query
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return View(list);
        }

        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // GET: Announcements/Create
        public IActionResult Create()
        {
            var model = new Announcement
            {
                Date = DateTime.Today,
                Priority = "medium"
            };
            return View(model);
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnnouncementId,ExternalId,Title,Body,Date,Priority,PostedBy,Expires")] Announcement announcement)
        {
            if (announcement.Date == default) announcement.Date = DateTime.Today;
            if (string.IsNullOrWhiteSpace(announcement.Priority)) announcement.Priority = "medium";

            if (ModelState.IsValid)
            {
                _context.Add(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Announcement '{announcement.Title}' was published successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // GET: Announcements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }
            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnnouncementId,ExternalId,Title,Body,Date,Priority,PostedBy,Expires")] Announcement announcement)
        {
            if (id != announcement.AnnouncementId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Announcement '{announcement.Title}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnouncementId))
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
            return View(announcement);
        }

        // GET: Announcements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? announcementId = null)
        {
            id = id != 0 ? id : (announcementId ?? 0);
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                var title = announcement.Title;
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Notice '{title}' was removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementId == id);
        }
    }
}
