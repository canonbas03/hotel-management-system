using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models;
using HotelMVCPrototype.Models.Enums;
using HotelMVCPrototype.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelMVCPrototype.Controllers
{
    [Authorize(Roles = "Reception, Admin")]

    public class GuestAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogger _audit;

        public GuestAssignmentsController(ApplicationDbContext context, IAuditLogger audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var assignments = await _context.GuestAssignments
                .Include(g => g.Room)
                .Include(g => g.Guests)
                .Where(g => g.IsActive)
                .ToListAsync();

            var availableRooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .OrderBy(r => r.Number)
                .ToListAsync();

            ViewBag.AvailableRooms = availableRooms;

            return View(assignments);
        }


        public IActionResult Create(int roomId)
        {
            var model = new CreateStayViewModel
            {
                RoomId = roomId,
                CheckInDate = DateTime.Today,
                Guests = new List<GuestInputViewModel>
                    {
                        new GuestInputViewModel(),
                    }
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStayViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room == null)
                return NotFound();

            var stay = new GuestAssignment
            {
                RoomId = model.RoomId,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                IsActive = true,
                Guests = new List<Guest>()
            };

            foreach (var g in model.Guests)
            {
                stay.Guests.Add(new Guest
                {
                    FirstName = g.FirstName,
                    LastName = g.LastName,
                    EGN = g.EGN,
                    BirthDate = g.BirthDate,
                    Sex = g.Sex,
                    Nationality = g.Nationality,
                    Phone = g.Phone
                });

               

            }

            room.Status = RoomStatus.Occupied;

            _context.GuestAssignments.Add(stay);
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                action: "GuestsAssignedToRoom",
                entityType: "GuestAssignment",
                entityId: stay.Id,
                description: $"Guests assigned to room {room.Number}",
                data: new
                {
                    RoomId = room.Id,
                    RoomNumber = room.Number,
                    CheckInDate = stay.CheckInDate,
                    CheckOutDate = stay.CheckOutDate,
                    Guests = stay.Guests.Select(x => new
                    {
                        x.Id,
                        x.FirstName,
                        x.LastName,
                        x.EGN
                    })
                }
            );


            return RedirectToAction("RoomDetails", "Reception", new { id = model.RoomId });
        }

        public IActionResult GuestTemplate()
        {
            return PartialView("_GuestInput", new GuestInputViewModel());
        }



        public async Task<IActionResult> CheckOut(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.GuestAssignments
                                .Include(g => g.Room)
                                .FirstOrDefaultAsync(g => g.Id == id);

            if (assignment == null) return NotFound();

            assignment.CheckOutDate = DateTime.Now;
            assignment.IsActive = false;

            assignment.Room.Status = Models.Enums.RoomStatus.Cleaning;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
