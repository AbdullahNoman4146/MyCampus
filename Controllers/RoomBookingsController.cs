using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyCampus.Data;
using MyCampus.Models;

namespace MyCampus.Controllers
{
    public class RoomBookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomBookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RoomBookings
        public async Task<IActionResult> Index(int? roomId, string? status)
        {
            var query = _context.RoomBookings
                .Include(b => b.Room)
                .AsQueryable();

            if (roomId.HasValue && roomId.Value > 0)
            {
                query = query.Where(b => b.RoomId == roomId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.Status == status);
            }

            ViewBag.RoomsList = new SelectList(await _context.Rooms.OrderBy(r => r.RoomNumber).ToListAsync(), "RoomId", "RoomNumber", roomId);
            ViewBag.SelectedStatus = status;

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ThenBy(b => b.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        // GET: RoomBookings/Create?roomId=5
        public async Task<IActionResult> Create(int? roomId)
        {
            await PopulateRoomsDropDownList(roomId);

            var model = new RoomBooking
            {
                BookingDate = DateTime.Today.AddDays(1),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Status = "Confirmed"
            };

            if (roomId.HasValue)
            {
                model.RoomId = roomId.Value;
            }

            return View(model);
        }

        // POST: RoomBookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,BookedBy,BookingDate,StartTime,EndTime,Purpose")] RoomBooking booking)
        {
            // Validation 1: Date must not be in the past
            if (booking.BookingDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
            }

            // Validation 2: EndTime must be after StartTime
            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }

            // Validation 3: Prevent double booking
            if (ModelState.IsValid)
            {
                var hasConflict = await _context.RoomBookings.AnyAsync(b =>
                    b.RoomId == booking.RoomId &&
                    b.BookingDate.Date == booking.BookingDate.Date &&
                    b.Status == "Confirmed" &&
                    booking.StartTime < b.EndTime &&
                    booking.EndTime > b.StartTime);

                if (hasConflict)
                {
                    ModelState.AddModelError("", "Conflict Detected: This room is already reserved for the selected date and time range. Please choose a different time slot or another room.");
                }
            }

            if (ModelState.IsValid)
            {
                booking.Status = "Confirmed";
                booking.CreatedAt = DateTime.UtcNow;

                _context.RoomBookings.Add(booking);
                await _context.SaveChangesAsync();

                var room = await _context.Rooms.FindAsync(booking.RoomId);
                TempData["SuccessMessage"] = $"Room {(room?.RoomNumber ?? "Booking")} successfully reserved for {booking.BookingDate:MMM dd, yyyy} ({booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm}).";
                return RedirectToAction(nameof(Index));
            }

            await PopulateRoomsDropDownList(booking.RoomId);
            return View(booking);
        }

        // POST: RoomBookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.RoomBookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Cancelled";
            _context.RoomBookings.Update(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking #{id} for {booking.Room?.RoomNumber ?? "Room"} has been cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateRoomsDropDownList(object? selectedRoom = null)
        {
            var roomsQuery = await _context.Rooms
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            ViewBag.RoomId = new SelectList(roomsQuery, "RoomId", "RoomNumber", selectedRoom);
        }
    }
}
