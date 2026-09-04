using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms with search/filtering by Capacity, Equipment, Type, and Availability
        public async Task<IActionResult> Index(int? capacity, string? equipment, string? roomType, string? availability)
        {
            var query = _context.Rooms.AsQueryable();

            if (capacity.HasValue && capacity.Value > 0)
            {
                query = query.Where(r => r.Capacity >= capacity.Value);
            }

            if (!string.IsNullOrWhiteSpace(equipment))
            {
                var eq = equipment.Trim().ToLower();
                query = query.Where(r => r.Equipment.ToLower().Contains(eq));
            }

            if (!string.IsNullOrWhiteSpace(roomType) && !roomType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var rt = roomType.Trim().ToLower();
                query = query.Where(r => r.Type != null && r.Type.ToLower() == rt);
            }

            if (!string.IsNullOrWhiteSpace(availability) && !availability.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var av = availability.Trim().ToLower();
                query = query.Where(r => r.BookingStatus.ToLower() == av);
            }

            ViewBag.SelectedCapacity = capacity;
            ViewBag.SelectedEquipment = equipment;
            ViewBag.SelectedRoomType = roomType;
            ViewBag.SelectedAvailability = availability;

            var rooms = await query.OrderBy(r => r.RoomNumber).ToListAsync();
            return View(rooms);
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,ExternalId,RoomNumber,Type,Capacity,Equipment,Floor,BookingStatus")] Room room)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(room.Type)) room.Type = "classroom";
                if (string.IsNullOrWhiteSpace(room.BookingStatus)) room.BookingStatus = "Available";
                if (room.Floor <= 0) room.Floor = 7;

                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,ExternalId,RoomNumber,Type,Capacity,Equipment,Floor,BookingStatus")] Room room)
        {
            if (id != room.RoomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId))
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
            return View(room);
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? roomId = null)
        {
            id = id != 0 ? id : (roomId ?? 0);
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                var roomNum = room.RoomNumber;
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Room '{roomNum}' was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }
    }
}
