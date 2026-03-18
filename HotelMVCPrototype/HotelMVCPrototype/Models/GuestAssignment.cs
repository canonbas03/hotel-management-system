using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelMVCPrototype.Models
{
    public class GuestAssignment
    {
        public int Id { get; set; }

        

        [Required]
        public int RoomId { get; set; }


        [ForeignKey("RoomId")]
        [ValidateNever] 
        public Room Room { get; set; }


        [Required]
        [Display(Name = "Check-In Date")]
        public DateTime CheckInDate { get; set; } = DateTime.Now;

        [Display(Name = "Check-Out Date")]
        public DateTime CheckOutDate { get; set; }

        [Display(Name = "Active Assignment")]
        public bool IsActive { get; set; } = true;

        public ICollection<Guest> Guests { get; set; } = new List<Guest>();
    }
}
