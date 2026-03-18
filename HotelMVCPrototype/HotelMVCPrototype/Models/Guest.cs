using HotelMVCPrototype.Models;
using System.ComponentModel.DataAnnotations;

public class Guest
{
    public int Id { get; set; }

    public int GuestAssignmentId { get; set; }
    public GuestAssignment GuestAssignment { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string EGN { get; set; }          
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Birth Date")]
    public DateTime BirthDate { get; set; }

    public string Sex { get; set; }          
    public Nationality? Nationality { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? DepartedAt { get; set; }
}
