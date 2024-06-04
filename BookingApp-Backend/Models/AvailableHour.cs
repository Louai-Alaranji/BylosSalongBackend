namespace BookingApp_Backend.Models
{
    public class AvailableHour
    {
        public int? Id { get; set; } // Unique identifier (primary key)
        public int ServiceId { get; set; } // Foreign key referencing Service.Id
        public DateTime Date { get; set; } // Date of the available hour
        public TimeSpan StartTime { get; set; } 
        public bool IsAvailable { get; set; } // Indicates if the hour is available for booking

    }
}
