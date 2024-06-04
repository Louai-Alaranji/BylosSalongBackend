namespace BookingApp_Backend.Models
{
    public class SetAvailableHoursRequest
    {
        public WorkingHours WorkingHours { get; set; }
        public int? ServiceId { get; set; }
        public DateTime Date {  get; set; }
    }
}
