using HotelMVCPrototype.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")] // or Admin
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
    string? user,
    string? auditAction,
    string? entityType,
    int? entityId,
    string? room,     
    string? description,   
    int days = 7)
    {
        var from = DateTime.Now.AddDays(-days);

        var q = _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= from);


        if (!string.IsNullOrWhiteSpace(user))
            q = q.Where(l => l.UserName != null && l.UserName.Contains(user));


        if (!string.IsNullOrWhiteSpace(auditAction))
            q = q.Where(l => l.Action.Contains(auditAction));


        if (!string.IsNullOrWhiteSpace(entityType))
            q = q.Where(l => l.EntityType.Contains(entityType));

        if (entityId.HasValue)
            q = q.Where(l => l.EntityId == entityId);

        if (!string.IsNullOrWhiteSpace(room))
        {
            if (int.TryParse(room, out var roomNumber))
            {
                q =
                    from l in q
                    join i in _context.RoomIssues.AsNoTracking() on l.EntityId equals i.Id
                    where l.EntityType == "RoomIssue"
                       && i.Room != null
                       && i.Room.Number == roomNumber
                    select l;
            }
            else
            {
                q = q.Where(_ => false);
            }
        }


        if (!string.IsNullOrWhiteSpace(description))
            q = q.Where(l => l.Description != null && l.Description.Contains(description));

        var logs = await q
            .OrderByDescending(l => l.CreatedAt)
            .Take(500)
            .ToListAsync();

        ViewBag.Days = days;
        ViewBag.User = user;
        ViewBag.AuditAction = auditAction;
        ViewBag.EntityType = entityType;
        ViewBag.EntityId = entityId;
        ViewBag.Room = room;
        ViewBag.Description = description;

        return View(logs);
    }


    public async Task<IActionResult> Details(int id)
    {
        var log = await _context.AuditLogs.FirstOrDefaultAsync(x => x.Id == id);
        if (log == null) return NotFound();
        return View(log);
    }

}
