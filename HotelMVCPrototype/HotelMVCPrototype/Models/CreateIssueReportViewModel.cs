using HotelMVCPrototype.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class CreateIssueReportViewModel
{
    public int? RoomId { get; set; }
    public int? RoomNumber { get; set; }

    [Required]
    public IssueCategory Category { get; set; }

    [Required]
    public string TypeKey { get; set; } = ""; 

    public string? Description { get; set; }

    public List<SelectListItem> Rooms { get; set; } = new();

    public List<SelectListItem> Types { get; set; } = new();
}

public enum IssueCategory
{
    Housekeeping,
    Maintenance,
    Security
}
