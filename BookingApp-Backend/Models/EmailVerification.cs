using System.ComponentModel.DataAnnotations;

namespace BookingApp_Backend.Models
{
    public class EmailVerification
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
