namespace BookingApp_Backend.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DurationInMinutes { get; set; }
        public int? EmployeeId { get; set; } // Foreign key referencing Employee.Id
        public ICollection<AvailableHour>? AvailableHours { get; set; }
    }
}
