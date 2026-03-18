using HotelMVCPrototype.Models.Enums;

namespace HotelMVCPrototype.Models
{
    public class RoomIssue
    {
        public int Id { get; set; }

        public int? RoomId { get; set; }
        public Room? Room { get; set; }

        public IssueCategory Category { get; set; }

        public string TypeKey { get; set; } = "";

        public string? Description { get; set; }

        public IssueStatus Status { get; set; } = IssueStatus.New;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }

        public string? ReportedByUserId { get; set; }
        public string? ReportedByUserName { get; set; }
    }
}
