namespace BookingApp_Backend.Models
{
    public class Register
    {

        public string Name { get; set; }
        public string Email { get; set; }
        public string? Job { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }

        public string? ImageName { get; set; } 
        public IFormFile? ImageFile { get; set; }

    }
}
