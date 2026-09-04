
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Models;
using MyCampus.Data;

public class CampusEventsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CampusEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CAMPUSEVENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Events.ToListAsync());
    }

    // GET: CAMPUSEVENTS/Details/5
    public async Task<IActionResult> Details(int? eventid)
    {
        if (eventid == null)
        {
            return NotFound();
        }

        var campusevent = await _context.Events
            .FirstOrDefaultAsync(m => m.EventId == eventid);
        if (campusevent == null)
        {
            return NotFound();
        }

        return View(campusevent);
    }

    // GET: CAMPUSEVENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CAMPUSEVENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("EventId,Name,Date,Time,Capacity")] CampusEvent campusevent)
    {
        if (ModelState.IsValid)
        {
            _context.Add(campusevent);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(campusevent);
    }

    // GET: CAMPUSEVENTS/Edit/5
    public async Task<IActionResult> Edit(int? eventid)
    {
        if (eventid == null)
        {
            return NotFound();
        }

        var campusevent = await _context.Events.FindAsync(eventid);
        if (campusevent == null)
        {
            return NotFound();
        }
        return View(campusevent);
    }

    // POST: CAMPUSEVENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? eventid, [Bind("EventId,Name,Date,Time,Capacity")] CampusEvent campusevent)
    {
        if (eventid != campusevent.EventId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(campusevent);
                await _context.SaveChangesAsync();
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

    // GET: CAMPUSEVENTS/Delete/5
    public async Task<IActionResult> Delete(int? eventid)
    {
        if (eventid == null)
        {
            return NotFound();
        }

        var campusevent = await _context.Events
            .FirstOrDefaultAsync(m => m.EventId == eventid);
        if (campusevent == null)
        {
            return NotFound();
        }

        return View(campusevent);
    }

    // POST: CAMPUSEVENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? eventid)
    {
        var campusevent = await _context.Events.FindAsync(eventid);
        if (campusevent != null)
        {
            _context.Events.Remove(campusevent);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CampusEventExists(int? eventid)
    {
        return _context.Events.Any(e => e.EventId == eventid);
    }
}
