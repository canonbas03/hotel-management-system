using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class SecurityIncidentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SecurityIncidentsController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Create(int? roomId)
    {
        var model = new CreateSecurityIncidentViewModel
        {
            RoomId = roomId
        };

        if (roomId != null)
        {
            var room = await _context.Rooms
                .Where(r => r.Id == roomId)
                .Select(r => new { r.Number })
                .FirstOrDefaultAsync();

            if (room != null)
            {
                model.RoomNumber = room.Number;
            }
        }

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSecurityIncidentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);

        var incident = new SecurityIncident
        {
            Type = model.Type,
            Description = model.Description,
            RoomId = model.RoomId,
            ReportedByUserId = user.Id
        };

        _context.SecurityIncidents.Add(incident);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }
}
