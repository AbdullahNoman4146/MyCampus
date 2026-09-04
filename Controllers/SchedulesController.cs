
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Models;
using MyCampus.Data;

public class SchedulesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SchedulesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SCHEDULES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Schedules.ToListAsync());
    }

    // GET: SCHEDULES/Details/5
    public async Task<IActionResult> Details(int? scheduleid)
    {
        if (scheduleid == null)
        {
            return NotFound();
        }

        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(m => m.ScheduleId == scheduleid);
        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    // GET: SCHEDULES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SCHEDULES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ScheduleId,Course,Time,Room,Day,Instructor")] Schedule schedule)
    {
        if (ModelState.IsValid)
        {
            _context.Add(schedule);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(schedule);
    }

    // GET: SCHEDULES/Edit/5
    public async Task<IActionResult> Edit(int? scheduleid)
    {
        if (scheduleid == null)
        {
            return NotFound();
        }

        var schedule = await _context.Schedules.FindAsync(scheduleid);
        if (schedule == null)
        {
            return NotFound();
        }
        return View(schedule);
    }

    // POST: SCHEDULES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? scheduleid, [Bind("ScheduleId,Course,Time,Room,Day,Instructor")] Schedule schedule)
    {
        if (scheduleid != schedule.ScheduleId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();
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

    // GET: SCHEDULES/Delete/5
    public async Task<IActionResult> Delete(int? scheduleid)
    {
        if (scheduleid == null)
        {
            return NotFound();
        }

        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(m => m.ScheduleId == scheduleid);
        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    // POST: SCHEDULES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? scheduleid)
    {
        var schedule = await _context.Schedules.FindAsync(scheduleid);
        if (schedule != null)
        {
            _context.Schedules.Remove(schedule);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ScheduleExists(int? scheduleid)
    {
        return _context.Schedules.Any(e => e.ScheduleId == scheduleid);
    }
}
