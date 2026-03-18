using HotelMVCPrototype.Data;
using HotelMVCPrototype.Hubs;
using HotelMVCPrototype.Models;
using HotelMVCPrototype.Models.Enums;
using HotelMVCPrototype.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HotelMVCPrototype.Controllers
{
    [Authorize(Roles = "Housekeeping, Admin")]
    public class HousekeepingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomStatisticsService _statsService;
        private readonly IHubContext<HotelHub> _hub;

        public HousekeepingController(ApplicationDbContext context, IRoomStatisticsService statsService, IHubContext<HotelHub> hub)
        {
            _context = context;
            _statsService = statsService;
            _hub = hub;
        }


        public async Task<IActionResult> Index(int floor = 1)
        {

            var allRoomsNeedingCleaning = await _context.Rooms
               .Where(r => r.Status == RoomStatus.Cleaning || r.NeedsDailyCleaning)
               .OrderBy(r => r.Floor)
               .ThenBy(r => r.Number)
               .ToListAsync();

            var floorRoomsNeedingCleaning = allRoomsNeedingCleaning
       .Where(r => r.Floor == floor)
       .ToList();



            var roomMap = floorRoomsNeedingCleaning
                .Select(r => new RoomMapViewModel
                {
                    RoomId = r.Id,
                    Number = r.Number,
                    TopPercent = r.MapTopPercent,
                    LeftPercent = r.MapLeftPercent,
                    WidthPercent = r.MapWidthPercent,
                    HeightPercent = r.MapHeightPercent,
                    StatusColor = "#0dcaf0",
                    IsDND = r.IsDND
                })
                .ToList();

            var currentUser = User.Identity?.Name;

            var today = DateTime.Now.Date;
            var todaysCleanings = await _context.CleaningLogs
                .Include(l => l.Room)
                .Where(l => l.CreatedAt.Date == today && l.Housekeeper == currentUser)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var roomIds = allRoomsNeedingCleaning.Select(r => r.Id).ToList();

            var openIssues = await _context.RoomIssues
                .Where(i =>
                    i.Category == IssueCategory.Housekeeping &&
                    i.Status != IssueStatus.Resolved &&
                    i.RoomId != null &&
                    roomIds.Contains(i.RoomId.Value))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var vm = new HousekeepingDashboardViewModel
            {
                Rooms = allRoomsNeedingCleaning,
                RoomStatistics = await _statsService.GetStatisticsAsync(),
                RoomMapPage = new RoomMapPageViewModel
                {
                    CurrentFloor = floor,
                    Mode = RoomMapMode.Housekeeping,
                    Rooms = roomMap
                },
                TodaysCleanings = todaysCleanings,
                OpenHousekeepingIssues = openIssues,
            };


            return View(vm);
        }



        [HttpPost]
        public async Task<IActionResult> MarkCleaned(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            if (room.Status != RoomStatus.Cleaning && !room.NeedsDailyCleaning)
            {
                return BadRequest("Room is not in cleaning state.");
            }

            if (room.Status == RoomStatus.Cleaning)
            {
                room.Status = RoomStatus.Available;
            }
            else if(room.NeedsDailyCleaning)
            {
                room.NeedsDailyCleaning = false;
            }

            _context.Rooms.Update(room);


            var log = new CleaningLog
            {
                RoomId = room.Id,
                CreatedAt = DateTime.Now,   
                Housekeeper = User?.Identity?.Name,
                Notes = null
            };
            _context.CleaningLogs.Add(log);


            var issue = await _context.RoomIssues
    .Where(i => i.RoomId == room.Id
             && i.Category == IssueCategory.Housekeeping
             && i.Status != IssueStatus.Resolved)
    .OrderByDescending(i => i.CreatedAt)
    .FirstOrDefaultAsync();

            if (issue != null)
            {
                issue.Status = IssueStatus.Resolved;
                issue.ResolvedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("RoomCleaned", room.Id);
            await _hub.Clients.All.SendAsync("CleaningLogged", log.Id);
            await _hub.Clients.All.SendAsync("RoomStatusChanged", new
            {
                RoomId = room.Id,
                Color = "#164E63",
                Stats = await _statsService.GetStatisticsAsync() 
            });



            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartHousekeepingDay()
        {
            var rooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Occupied)
                .ToListAsync();

            foreach (var room in rooms)
            {
                room.NeedsDailyCleaning = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
