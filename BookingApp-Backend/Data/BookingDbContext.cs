using BookingApp_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BookingApp_Backend.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<AvailableHour> AvailableHours { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        // Define entity configurations (relationships, etc.) if needed
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public void DeleteOldBookingsAndAvailableHours()
        {
            

            DateTime thresholdDate = DateTime.Today.AddDays(-365); // Get the date from one day ago

            // Delete old booking requests
            var oldBookings = BookingRequests.Where(b => b.Date < thresholdDate).ToList();
            BookingRequests.RemoveRange(oldBookings);
            
            // Delete old available hours
            var oldAvailableHours = AvailableHours.Where(a => a.Date < thresholdDate).ToList();
            AvailableHours.RemoveRange(oldAvailableHours);

            SaveChanges(); // Save changes to the database
        }


    }
}
