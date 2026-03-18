using HotelMVCPrototype.Data;
using HotelMVCPrototype.Hubs;
using HotelMVCPrototype.Models;
using HotelMVCPrototype.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class GuestRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<HotelHub> _hub;

    public GuestRequestsController(ApplicationDbContext context, IHubContext<HotelHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task<IActionResult> Index(int roomId)
    {
        ViewBag.RoomId = roomId;
        ViewBag.RoomNumber = _context.Rooms.FirstOrDefault(r => r.Id == roomId).Number;

        var items = await _context.RequestItems
       .Where(i => i.IsActive)
       .ToListAsync();

        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceRequest(
        int roomId,
        Dictionary<int, int> items)
    {
        if (items == null || !items.Any(i => i.Value > 0))
            return BadRequest("No items selected.");

        var dbItems = await _context.RequestItems
            .Where(i => items.Keys.Contains(i.Id))
            .ToListAsync();

        foreach (var dbItem in dbItems)
        {
            int requestedQty = items[dbItem.Id];

            if (dbItem.MaxQuantity.HasValue &&
                requestedQty > dbItem.MaxQuantity.Value)
            {
                return BadRequest(
                    $"You can request maximum {dbItem.MaxQuantity} of '{dbItem.Name}'."
                );
            }
        }

        var request = new ServiceRequest
        {
            RoomId = roomId,
            Status = ServiceRequestStatus.New,
            Items = new List<ServiceRequestItem>()
        };

        foreach (var item in items.Where(i => i.Value > 0))
        {
            request.Items.Add(new ServiceRequestItem
            {
                RequestItemId = item.Key,
                Quantity = item.Value
            });
        }

        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("NewRequest", roomId);

        return RedirectToAction(nameof(ThankYou), new { id = request.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ThankYou(int id)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Items)
            .ThenInclude(i => i.RequestItem)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound();

        return View(request);
    }
}
