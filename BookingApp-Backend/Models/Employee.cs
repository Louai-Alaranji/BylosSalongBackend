using System.ComponentModel.DataAnnotations.Schema;

namespace BookingApp_Backend.Models
{
    public class Employee
    {
        public int Id { get; set; } // Unique identifier (primary key)
        public string? Name { get; set; }
        public string? Job{ get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? PasswordHash { get; set; }
        public string? ImageName { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public ICollection<Service>? Services { get; set; }

        public ICollection<BookingRequest>? Bookings { get; set; }
    }
}