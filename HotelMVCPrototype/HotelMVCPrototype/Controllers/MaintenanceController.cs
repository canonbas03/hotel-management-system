using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models.Enums;
using HotelMVCPrototype.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelMVCPrototype.Controllers
{
    [Authorize(Roles = "Maintenance, Admin")]
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogger _audit;

        public MaintenanceController(ApplicationDbContext context, IAuditLogger audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var issues = await _context.RoomIssues
                .Include(i => i.Room)
                .Where(i => i.Category == IssueCategory.Maintenance &&
                            i.Status != IssueStatus.Resolved)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(issues);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Acknowledge(int id)
        {
            var issue = await _context.RoomIssues
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();
            if (issue.Category != IssueCategory.Maintenance) return BadRequest();

            issue.Status = IssueStatus.InProgress;
            await _context.SaveChangesAsync();

            var roomLabel = issue.Room != null ? $"Room {issue.Room.Number}" : "No room";

            await _audit.LogAsync(
                action: "MaintenanceAcknowledged",
                entityType: "RoomIssue",
                entityId: issue.Id,
                description: $"Maintenance acknowledged issue: [{issue.TypeKey}] ({roomLabel})",
                data: new
                {
                    issue.Id,
                    issue.RoomId,
                    RoomNumber = issue.Room?.Number,
                    issue.TypeKey,
                    NewStatus = issue.Status.ToString(),
                    issue.CreatedAt
                }
            );

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var issue = await _context.RoomIssues
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();
            if (issue.Category != IssueCategory.Maintenance) return BadRequest();

            issue.Status = IssueStatus.Resolved;
            issue.ResolvedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var roomLabel = issue.Room != null ? $"Room {issue.Room.Number}" : "No room";

            await _audit.LogAsync(
                action: "MaintenanceResolved",
                entityType: "RoomIssue",
                entityId: issue.Id,
                description: $"Maintenance resolved issue: [{issue.TypeKey}] ({roomLabel})",
                data: new
                {
                    issue.Id,
                    issue.RoomId,
                    RoomNumber = issue.Room?.Number,
                    issue.TypeKey,
                    issue.ResolvedAt,
                    issue.Description
                }
            );

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarkDone(int issueId) => Resolve(issueId);
    }
}
