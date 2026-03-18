using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models;
using HotelMVCPrototype.Models.Enums;
using HotelMVCPrototype.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelMVCPrototype.Controllers
{
    [Authorize(Roles = "Reception, Admin")]
    public class ReceptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomStatisticsService _statsService;
        private readonly IAuditLogger _audit;

        public ReceptionController(ApplicationDbContext context, IRoomStatisticsService statsService, IAuditLogger audit)
        {
            _context = context;
            _statsService = statsService;
            _audit = audit;
        }


        public async Task<IActionResult> Index(int floor = 1)
        {
            var rooms = await _context.Rooms
                .Include(r => r.GuestAssignments.Where(g => !g.IsActive))
                .ToListAsync();

            //var requests = await _context.ServiceRequests
            //    .Include(r => r.Room)
            //    .Include(r => r.Items)
            //    .ThenInclude(i => i.RequestItem)
            //    .Where(r => r.Status == ServiceRequestStatus.New)
            //    .OrderBy(r => r.CreatedAt)
            //    .Take(3) // side window, not full list
            //    .ToListAsync();

            var roomsWithRequests = await _context.ServiceRequests
                .Where(r => r.Status == ServiceRequestStatus.New)
                .Select(r => r.RoomId)
                .Distinct()
                .ToListAsync();

            var roomMap = rooms
                .Where(r => r.Floor == floor)
                .Select(r => new RoomMapViewModel
                {
                    RoomId = r.Id,
                    Number = r.Number,
                    TopPercent = r.MapTopPercent,
                    LeftPercent = r.MapLeftPercent,
                    WidthPercent = r.MapWidthPercent,
                    HeightPercent = r.MapHeightPercent,
                    StatusColor = r.Status switch
                    {
                        RoomStatus.Available => "#164E63",   
                        RoomStatus.Occupied => "#86A789",    
                        RoomStatus.Cleaning => "#e8c53a",    
                        RoomStatus.Maintenance => "#DC3545", 
                        _ => "#FFFFFF"
                    },
                    IsDND = r.IsDND,
                    HasOpenRequest = roomsWithRequests.Contains(r.Id)
                })
                .ToList();


            var vm = new ReceptionDashboardViewModel
            {
                Rooms = rooms,
                RoomStatistics = await _statsService.GetStatisticsAsync(),
                RoomMap = roomMap,
                CurrentFloor = floor
            };

            return View(vm);
        }

        public async Task<IActionResult> CheckIn(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || room.Status != RoomStatus.Available)
                return BadRequest("Room is not available.");

            var assignment = new GuestAssignment
            {
                RoomId = room.Id,
                CheckInDate = DateTime.Now
            };

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(GuestAssignment assignment)
        {
            if (!ModelState.IsValid)
            {
                return View(assignment);
            }

            var room = await _context.Rooms.FindAsync(assignment.RoomId);
            if (room == null || room.Status != RoomStatus.Available)
            {
                ModelState.AddModelError("", "Room is not available.");
                return View(assignment);
            }

            room.Status = RoomStatus.Occupied;
            assignment.Room = room;
            _context.GuestAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                action: "RoomCheckedIn",
                entityType: "GuestAssignment",
                entityId: assignment.Id,
                description: $"Room {room.Number} checked in",
                data: new
                {
                    assignment.RoomId,
                    assignment.CheckInDate
                }
            );


            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> RoomDetails(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.GuestAssignments
                    .Where(g => g.IsActive))
                .ThenInclude(ga => ga.Guests)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound();

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Depart(int assignmentId)
        {
            var stay = await _context.GuestAssignments
                .Include(g => g.Room)
                .Include(ga => ga.Guests)
                .FirstOrDefaultAsync(g => g.Id == assignmentId && g.IsActive);

            if (stay == null)
                return NotFound();

            stay.IsActive = false;
            stay.CheckOutDate = DateTime.Now;

            foreach (var g in stay.Guests)
            {
                if (g.IsActive)
                {
                    g.IsActive = false;
                    g.DepartedAt = DateTime.Now;
                }
            }

            stay.Room.Status = RoomStatus.Cleaning;

            await _context.SaveChangesAsync();

            return RedirectToAction("RoomDetails", new { id = stay.RoomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartGuest(int guestId)
        {
            var guest = await _context.Guests
                .Include(g => g.GuestAssignment)
                    .ThenInclude(ga => ga.Room)
                .FirstOrDefaultAsync(g => g.Id == guestId);

            if (guest == null) return NotFound();

            if (guest.IsActive)
            {
                guest.IsActive = false;
                guest.DepartedAt = DateTime.Now;
            }

            var stay = await _context.GuestAssignments
                .Include(ga => ga.Room)
                .Include(ga => ga.Guests)
                .FirstOrDefaultAsync(ga => ga.Id == guest.GuestAssignmentId && ga.IsActive);

            if (stay != null)
            {
                if (stay.Guests.All(g => !g.IsActive))
                {
                    stay.IsActive = false;
                    stay.CheckOutDate = DateTime.Now;
                    stay.Room.Status = RoomStatus.Cleaning;
                }
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                action: "GuestDeparted",
                entityType: "Guest",
                entityId: guest.Id,
                description: $"Guest {guest.FirstName} {guest.LastName} departed room {stay.Room.Number}",
                data: new
                {
                    guest.GuestAssignmentId,
                    RoomId = stay.Room.Id,
                    DepartedAt = guest.DepartedAt
                }
            );


            return RedirectToAction("RoomDetails", new { id = guest.GuestAssignment.RoomId });
        }

        public async Task<IActionResult> RoomHistory(int roomId)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null) return NotFound();

            var assignments = await _context.GuestAssignments
                .Where(ga => ga.RoomId == roomId)
                .Include(ga => ga.Guests)
                .OrderByDescending(ga => ga.CheckInDate)
                .ToListAsync();

            var vm = new RoomHistoryViewModel
            {
                RoomId = room.Id,
                RoomNumber = room.Number,
                Assignments = assignments
            };

            return View(vm);
        }

    }
}
