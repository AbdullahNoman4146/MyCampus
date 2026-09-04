using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Controllers
{
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Assignments
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.Assignments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var st = status.Trim().ToLower();
                query = query.Where(a => a.Status.ToLower() == st);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a => a.Course.ToLower().Contains(term) ||
                                         (a.CourseTitle != null && a.CourseTitle.ToLower().Contains(term)) ||
                                         a.Title.ToLower().Contains(term) ||
                                         (a.Description != null && a.Description.ToLower().Contains(term)) ||
                                         (a.SubmissionPlatform != null && a.SubmissionPlatform.ToLower().Contains(term)));
            }

            ViewBag.SelectedStatus = status;
            ViewBag.SearchTerm = search;

            var list = await query
                .OrderBy(a => a.Deadline)
                .ToListAsync();

            return View(list);
        }

        // GET: Assignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(m => m.AssignmentId == id);
            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        // GET: Assignments/Create
        public IActionResult Create()
        {
            var model = new Assignment
            {
                AssignedDate = DateTime.Today,
                Deadline = DateTime.Today.AddDays(7),
                Status = "pending",
                SubmissionPlatform = "Google Classroom",
                Marks = 10
            };
            return View(model);
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AssignmentId,ExternalId,Course,CourseTitle,Title,Description,AssignedDate,Deadline,SubmissionPlatform,Status,Marks")] Assignment assignment)
        {
            if (assignment.AssignedDate == default) assignment.AssignedDate = DateTime.Today;
            if (string.IsNullOrWhiteSpace(assignment.Status)) assignment.Status = "pending";

            if (ModelState.IsValid)
            {
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Assignment '{assignment.Title}' for {assignment.Course} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(assignment);
        }

        // GET: Assignments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }
            return View(assignment);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AssignmentId,ExternalId,Course,CourseTitle,Title,Description,AssignedDate,Deadline,SubmissionPlatform,Status,Marks")] Assignment assignment)
        {
            if (id != assignment.AssignmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Assignment '{assignment.Title}' updated successfully!";
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

        // GET: Assignments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(m => m.AssignmentId == id);
            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? assignmentId = null)
        {
            id = id != 0 ? id : (assignmentId ?? 0);
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                var title = assignment.Title;
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Assignment '{title}' was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.AssignmentId == id);
        }
    }
}
