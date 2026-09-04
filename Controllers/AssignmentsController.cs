
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Models;
using MyCampus.Data;

public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AssignmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ASSIGNMENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Assignments.ToListAsync());
    }

    // GET: ASSIGNMENTS/Details/5
    public async Task<IActionResult> Details(int? assignmentid)
    {
        if (assignmentid == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(m => m.AssignmentId == assignmentid);
        if (assignment == null)
        {
            return NotFound();
        }

        return View(assignment);
    }

    // GET: ASSIGNMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ASSIGNMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AssignmentId,Course,Title,Deadline,Status")] Assignment assignment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(assignment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(assignment);
    }

    // GET: ASSIGNMENTS/Edit/5
    public async Task<IActionResult> Edit(int? assignmentid)
    {
        if (assignmentid == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments.FindAsync(assignmentid);
        if (assignment == null)
        {
            return NotFound();
        }
        return View(assignment);
    }

    // POST: ASSIGNMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? assignmentid, [Bind("AssignmentId,Course,Title,Deadline,Status")] Assignment assignment)
    {
        if (assignmentid != assignment.AssignmentId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(assignment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(assignment.AssignmentId))
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
        return View(assignment);
    }

    // GET: ASSIGNMENTS/Delete/5
    public async Task<IActionResult> Delete(int? assignmentid)
    {
        if (assignmentid == null)
        {
            return NotFound();
        }

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(m => m.AssignmentId == assignmentid);
        if (assignment == null)
        {
            return NotFound();
        }

        return View(assignment);
    }

    // POST: ASSIGNMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? assignmentid)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentid);
        if (assignment != null)
        {
            _context.Assignments.Remove(assignment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AssignmentExists(int? assignmentid)
    {
        return _context.Assignments.Any(e => e.AssignmentId == assignmentid);
    }
}
