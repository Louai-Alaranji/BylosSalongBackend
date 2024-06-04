namespace BookingApp_Backend.Models
{
    public class BookingRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; } // ID of the employee to book with
        public int ServiceId { get; set; } // ID of the service being booked
        public DateTime Date { get; set; } // Date of the appointment
        public TimeSpan StartTime { get; set; } // Start time of the appointment
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        
    }
}
