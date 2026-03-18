using System.ComponentModel.DataAnnotations;

namespace HotelMVCPrototype.Models.ViewModels
{
    public class CreateStaffUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required]
        public string SelectedRole { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
