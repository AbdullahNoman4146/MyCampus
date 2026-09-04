
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Models;
using MyCampus.Data;

public class AnnouncementsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AnnouncementsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ANNOUNCEMENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Announcements.ToListAsync());
    }

    // GET: ANNOUNCEMENTS/Details/5
    public async Task<IActionResult> Details(int? announcementid)
    {
        if (announcementid == null)
        {
            return NotFound();
        }

        var announcement = await _context.Announcements
            .FirstOrDefaultAsync(m => m.AnnouncementId == announcementid);
        if (announcement == null)
        {
            return NotFound();
        }

        return View(announcement);
    }

    // GET: ANNOUNCEMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ANNOUNCEMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AnnouncementId,Title,Body,Date,Priority")] Announcement announcement)
    {
        if (ModelState.IsValid)
        {
            _context.Add(announcement);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(announcement);
    }

    // GET: ANNOUNCEMENTS/Edit/5
    public async Task<IActionResult> Edit(int? announcementid)
    {
        if (announcementid == null)
        {
            return NotFound();
        }

        var announcement = await _context.Announcements.FindAsync(announcementid);
        if (announcement == null)
        {
            return NotFound();
        }
        return View(announcement);
    }

    // POST: ANNOUNCEMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? announcementid, [Bind("AnnouncementId,Title,Body,Date,Priority")] Announcement announcement)
    {
        if (announcementid != announcement.AnnouncementId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(announcement);
                await _context.SaveChangesAsync();
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

    // GET: ANNOUNCEMENTS/Delete/5
    public async Task<IActionResult> Delete(int? announcementid)
    {
        if (announcementid == null)
        {
            return NotFound();
        }

        var announcement = await _context.Announcements
            .FirstOrDefaultAsync(m => m.AnnouncementId == announcementid);
        if (announcement == null)
        {
            return NotFound();
        }

        return View(announcement);
    }

    // POST: ANNOUNCEMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? announcementid)
    {
        var announcement = await _context.Announcements.FindAsync(announcementid);
        if (announcement != null)
        {
            _context.Announcements.Remove(announcement);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AnnouncementExists(int? announcementid)
    {
        return _context.Announcements.Any(e => e.AnnouncementId == announcementid);
    }
}
