namespace HotelMVCPrototype.Services
{
    using HotelMVCPrototype.Data;
    using HotelMVCPrototype.Services.Interfaces;
    using Microsoft.AspNetCore.Http;
    using System.Text.Json;

    public class AuditLogger : IAuditLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;

        public AuditLogger(ApplicationDbContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        public async Task LogAsync(string action, string entityType, int? entityId = null,
                                   string? description = null, object? data = null)
        {
            var ctx = _http.HttpContext;

            var log = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,

                UserId = ctx?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = ctx?.User?.Identity?.Name,
                Role = null,

                IpAddress = ctx?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = ctx?.Request?.Headers["User-Agent"].ToString(),

                DataJson = data == null ? null : JsonSerializer.Serialize(data)
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

}
