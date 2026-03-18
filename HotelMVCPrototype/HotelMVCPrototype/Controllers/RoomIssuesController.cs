using HotelMVCPrototype.Data;
using HotelMVCPrototype.Hubs;
using HotelMVCPrototype.Models;
using HotelMVCPrototype.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


public class RoomIssuesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHubContext<HotelHub> _hub;

    public RoomIssuesController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHubContext<HotelHub> hub)
    {
        _context = context;
        _userManager = userManager;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> Create(IssueCategory category, int? roomId)
    {
        var vm = new CreateRoomIssueViewModel
        {
            Category = category,
            RoomId = roomId
        };

        if (roomId.HasValue)
        {
            vm.RoomNumber = await _context.Rooms
                .Where(r => r.Id == roomId.Value)
                .Select(r => (int?)r.Number)
                .FirstOrDefaultAsync();
        }

        if (!roomId.HasValue)
        {
            ViewBag.Rooms = await _context.Rooms
                .OrderBy(r => r.Number)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"Room {r.Number}"
                })
                .ToListAsync();
        }

        ViewBag.TypeOptions = GetTypeOptions(category);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoomIssueViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TypeOptions = GetTypeOptions(vm.Category);

            if (!vm.RoomId.HasValue)
            {
                ViewBag.Rooms = await _context.Rooms
                    .OrderBy(r => r.Number)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"Room {r.Number}"
                    })
                    .ToListAsync();
            }

            return View(vm);
        }

        var user = await _userManager.GetUserAsync(User);

        var issue = new RoomIssue
        {
            RoomId = vm.RoomId,
            Category = vm.Category,
            TypeKey = vm.TypeKey,
            Description = vm.Description,
            Status = IssueStatus.New,
            ReportedByUserId = user?.Id,
            ReportedByUserName = User.Identity?.Name
        };

        _context.RoomIssues.Add(issue);

        if (vm.Category == IssueCategory.Maintenance && vm.RoomId.HasValue)
        {
            var room = await _context.Rooms.FindAsync(vm.RoomId.Value);
            if (room != null)
                room.Status = RoomStatus.Maintenance;
        }

        if (vm.Category == IssueCategory.Housekeeping && vm.RoomId.HasValue)
        {
            var room = await _context.Rooms.FindAsync(vm.RoomId.Value);
            if (room != null)
                room.NeedsDailyCleaning = true;
        }

        await _context.SaveChangesAsync();

        int? roomNumber = null;
        if (issue.RoomId.HasValue)
        {
            roomNumber = await _context.Rooms
                .Where(r => r.Id == issue.RoomId.Value)
                .Select(r => (int?)r.Number)
                .FirstOrDefaultAsync();
        }

        await _hub.Clients.All.SendAsync("RoomIssueCreated", new
        {
            IssueId = issue.Id,
            Category = issue.Category.ToString(),   // "Maintenance", "Housekeeping", "Security"
            RoomId = issue.RoomId,
            RoomNumber = roomNumber,
            TypeKey = issue.TypeKey,
            Description = issue.Description,
            Status = issue.Status.ToString(),
            CreatedAt = DateTime.Now
        });


        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id)
    {
        var issue = await _context.RoomIssues
            .Include(i => i.Room)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue == null) return NotFound();

        issue.Status = IssueStatus.Resolved;
        issue.ResolvedAt = DateTime.Now;

        if (issue.Category == IssueCategory.Maintenance && issue.RoomId.HasValue)
        {
            bool hasOtherOpen = await _context.RoomIssues.AnyAsync(i =>
                i.RoomId == issue.RoomId &&
                i.Category == IssueCategory.Maintenance &&
                i.Status != IssueStatus.Resolved &&
                i.Id != issue.Id);

            if (!hasOtherOpen && issue.Room != null)
                issue.Room.Status = RoomStatus.Available;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index)); 
    }

    private static List<SelectListItem> GetTypeOptions(IssueCategory category)
    {
        return category switch
        {
            IssueCategory.Maintenance => new()
            {
                new("Burst pipe", "BurstPipe"),
                new("AC not working", "ACNotWorking"),
                new("No hot water", "NoHotWater"),
                new("Electrical issue", "ElectricalIssue"),
                new("Other", "Other")
            },
            IssueCategory.Housekeeping => new()
            {
                new("Spill / stain", "Spill"),
                new("Extra towels", "ExtraTowels"),
                new("Extra bedding", "ExtraBedding"),
                new("Room needs cleaning", "DailyClean"),
                new("Other", "Other")
            },
            IssueCategory.Security => new()
            {
                new("Weird noise", "WeirdNoise"),
                new("Scream / shouting", "Scream"),
                new("Breaking things", "BreakingThings"),
                new("Suspicious person", "SuspiciousPerson"),
                new("Other", "Other")
            },
            _ => new() { new("Other", "Other") }
        };
    }

    public async Task<IActionResult> History(IssueCategory? category, int days = 7)
    {
        var from = DateTime.Now.AddDays(-days);

        var q = _context.RoomIssues
            .Include(i => i.Room)
            .Where(i => i.Status == IssueStatus.Resolved && i.ResolvedAt != null)
            .Where(i => i.ResolvedAt >= from);

        if (category.HasValue)
            q = q.Where(i => i.Category == category.Value);

        var resolved = await q
            .OrderByDescending(i => i.ResolvedAt)
            .Take(500)
            .ToListAsync();

        ViewBag.Days = days;
        ViewBag.Category = category;

        return View(resolved);
    }
}
