using HotelMVCPrototype.Data;
using HotelMVCPrototype.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class GuestRoomController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<HotelHub> _hub;

    public GuestRoomController(ApplicationDbContext context, IHubContext<HotelHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task<IActionResult> Index(int roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.GuestAssignments)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        return View(room);
    }

    // QR link landing page (MIGHT BE UNNECESARRY)
    public async Task<IActionResult> Room(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound();

        return View(room);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleDnd(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound();

        room.IsDND = !room.IsDND;
        await _context.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("DndChanged", room.Id, room.IsDND);

        return Content(room.IsDND ? "true" : "false");

    }
}
