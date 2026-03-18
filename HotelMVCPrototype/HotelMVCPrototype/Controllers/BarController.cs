using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models;
using HotelMVCPrototype.Models.Enums;
using HotelMVCPrototype.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Bar, Admin")]
public class BarController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogger _audit;

    public BarController(ApplicationDbContext context, IAuditLogger audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
             .Include(o => o.Room)
             .Include(o => o.Items)
             .ThenInclude(i => i.MenuItem)
             .Where(o => o.Status == OrderStatus.New)
             .OrderBy(o => o.CreatedAt)
             .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Complete(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        await _audit.LogAsync(
            action: "OrderCompleted",
            entityType: "Order",
            entityId: order.Id,
            description: $"Order {order.Id} completed",
            data: new { order.RoomId, order.CompletedAt }
        );

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History(int? days)
    {
        var query = _context.Orders
            .Include(o => o.Room)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Where(o => o.Status == OrderStatus.Completed);

        if(days.HasValue)
        {
            var fromDate = DateTime.Now.AddDays(-days.Value);
            query = query
                .Where(o => o.CompletedAt >= fromDate); 
        }

        var completedOrders = await query
            .OrderByDescending(o => o.CompletedAt)
            .ToListAsync();

        ViewBag.SelectedDays = days;

        return View(completedOrders);
    }
}
