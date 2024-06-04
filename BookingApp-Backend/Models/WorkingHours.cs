namespace BookingApp_Backend.Models
{
    public class WorkingHours
    {
        public TimeSpan StartTime { get; set; } // Start time of the work
        public TimeSpan LunchBreakStart { get; set; } // Start time of the lunch break
        public TimeSpan LunchBreakEnd { get; set; } // End time of the lunch break
        public TimeSpan EndTime { get; set; } // End time of the work
    }
}
