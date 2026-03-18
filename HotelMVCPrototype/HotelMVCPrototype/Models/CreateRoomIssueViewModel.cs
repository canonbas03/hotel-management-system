public class CreateRoomIssueViewModel
{
    public int? RoomId { get; set; }
    public int? RoomNumber { get; set; }

    public IssueCategory Category { get; set; }

    public string TypeKey { get; set; } = "";

    public string? Description { get; set; }
}
