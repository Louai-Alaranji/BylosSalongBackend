using BookingApp_Backend.Data;
using BookingApp_Backend.Helpers;
using BookingApp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace BookingApp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly BookingDbContext _bookingDbContext;
        private readonly IEmailSender _emailSender;
        public AdminController(BookingDbContext context, IEmailSender emailService)
        {
            _bookingDbContext = context;
            _emailSender = emailService;
        }


        [HttpPost("employees/{employeeId}/setAvailablehours")]
        public async Task<IActionResult> SetAvailableHours([FromRoute] int employeeId, [FromBody] SetAvailableHoursRequest requestData)
        {

            if (requestData == null || requestData.WorkingHours == null ||
                requestData.WorkingHours.StartTime == null ||
                requestData.WorkingHours.LunchBreakStart == null ||
                requestData.WorkingHours.LunchBreakEnd == null ||
                requestData.WorkingHours.EndTime == null ||
                requestData.ServiceId == null || requestData.Date == null)
            {
                return BadRequest("Invalid request data provided.");
            }

            // Fetch the employee
            var employee = await _bookingDbContext.Employees
                .Include(e => e.Services)
                .SingleOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return NotFound("Employee not found.");
            }

            // Find the service by id
            //var service = employee.Services.FirstOrDefault(s => s.Id == requestData.ServiceId);
            var service = await _bookingDbContext.Services
            .Include(s => s.AvailableHours)
            .FirstOrDefaultAsync(s => s.Id == requestData.ServiceId);

            if (service == null)
            {
                return BadRequest("Service not found.");
            }
            var existingHours = service.AvailableHours.Where(ah =>
      ah.Date == requestData.Date && ah.ServiceId == requestData.ServiceId).ToList();
            _bookingDbContext.AvailableHours.RemoveRange(existingHours);
            await _bookingDbContext.SaveChangesAsync();
            //service.AvailableHours.Clear();
            // Clear existing available hours for the service
            if (service.AvailableHours == null)
            {
                //return BadRequest("Service not found.");
                service.AvailableHours = new List<AvailableHour>(); // Initialize it if null
            }
      

            // Parse start time, lunch break, and end time
            var startTime = DateTime.Today.Add(new TimeSpan(requestData.WorkingHours.StartTime.Hours, requestData.WorkingHours.StartTime.Minutes, 0));
            var lunchBreakStart = DateTime.Today.Add(new TimeSpan(requestData.WorkingHours.LunchBreakStart.Hours, requestData.WorkingHours.LunchBreakStart.Minutes, 0));
            var lunchBreakEnd = DateTime.Today.Add(new TimeSpan(requestData.WorkingHours.LunchBreakEnd.Hours, requestData.WorkingHours.LunchBreakEnd.Minutes, 0));
            var endTime = DateTime.Today.Add(new TimeSpan(requestData.WorkingHours.EndTime.Hours, requestData.WorkingHours.EndTime.Minutes, 0));


            // Divide the working hours into 15-minute segments
            var currentHour = startTime;
            while (currentHour < endTime)
            {
                // Skip lunch break hour
                if (currentHour >= lunchBreakStart && currentHour < lunchBreakEnd)
                {
                    currentHour = lunchBreakEnd;
                    continue;
                }

                // Create an available hour for each 15-minute segment
                var availableHour = new AvailableHour
                {
                    ServiceId = service.Id,
                    Date = requestData.Date,
                    StartTime = currentHour.TimeOfDay,
                    IsAvailable = true
                };

                // Add the available hour to the service's collection
                service.AvailableHours.Add(availableHour);

                // Move to the next 15-minute segment
                currentHour = currentHour.AddMinutes(15);
            }

            try
            {
                await _bookingDbContext.SaveChangesAsync();
                return Ok("Available hours updated successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                Console.WriteLine($"Error saving available hours: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }




        [HttpDelete("DeleteSelectedHour")]
        public async Task<IActionResult> DeleteSelectedHour(AvailableHour hourObject)
        {
            try
            {
                // Find the service associated with the hour object
                var service = await _bookingDbContext.Services
                    .Include(s => s.AvailableHours)
                    .FirstOrDefaultAsync(s => s.AvailableHours.Any(ah => ah.Id == hourObject.Id));

                if (service == null)
                {
                    return NotFound("Service not found.");
                }

                // Find the hour object to delete
                var hourToDelete = service.AvailableHours.FirstOrDefault(ah => ah.Id == hourObject.Id);

                if (hourToDelete == null)
                {
                    return NotFound("Hour object not found.");
                }

                // Remove the hour object from the collection
                service.AvailableHours.Remove(hourToDelete);

                // Save changes to the database
                await _bookingDbContext.SaveChangesAsync();

                return Ok("Hour object deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting hour object: {ex.Message}");
            }
        }


        [AllowAnonymous] //GET HOURS CORRECTLY AND DISPLAY THEM
        [HttpGet("employees/{employeeId}/getavailablehours")]
        public async Task<IActionResult> GetAvailableHours([FromRoute] int employeeId)
        {
            try
            {
                // Fetch the employee with associated services
                var employee = await _bookingDbContext.Employees
                    .Include(e => e.Services)
                    .ThenInclude(s => s.AvailableHours)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                {
                    return NotFound("Employee not found.");
                }

                // Combine available hours from all services associated with the employee
                var availableHours = employee.Services
                    .SelectMany(s => s.AvailableHours)
                    .ToList();

                return Ok(availableHours);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving available hours: {ex.Message}");
            }
        }

        [AllowAnonymous]
        [HttpGet("getEmployee/{employeeId}")]
        public async Task<IActionResult> GetEmployee(int employeeId)
        {
            // Logic to retrieve the employee by ID from the database
            var employee = await _bookingDbContext.Employees
                      .Include(e => e.Services) // Eager load
                      .SingleOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return NotFound("Employee not found.");
            }

            return Ok(employee); // Return the retrieved employee object
        }

        
        [HttpDelete("DeleteEmployee/{employeeId}")]
        public async Task<IActionResult> DeleteEmployee(int employeeId)
        {
            try
            {
                // Find the employee to delete
                var employeeToDelete = await _bookingDbContext.Employees
                    .Include(e => e.Services)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employeeToDelete == null)
                {
                    return NotFound("Employee not found.");
                }

                // Delete the employee's picture if it exists
                if (!string.IsNullOrEmpty(employeeToDelete.ImageName))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "images", employeeToDelete.ImageName);
                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath);
                            Console.WriteLine($"Image deleted: {imagePath}"); // Add logging
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting image: {ex.Message}"); // Log errors
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Image not found: {imagePath}"); // Log if image doesn't exist
                    }
                }
                else
                {
                    Console.WriteLine("Employee has no image to delete."); // Log if no image associated
                }

                // Remove the employee from the database
                _bookingDbContext.Services.RemoveRange(employeeToDelete.Services);
                _bookingDbContext.Employees.Remove(employeeToDelete);

                // Save changes to the database
                await _bookingDbContext.SaveChangesAsync();

                return Ok("Employee deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting employee: {ex.Message}");
            }
        }


        [HttpGet("GetBookings/{employeeId}")]
        public async Task<IActionResult> GetBookings(int employeeId)
        {
            try
            {
                // Find the employee with the specified ID
                var employee = await _bookingDbContext.Employees
                    .Include(e => e.Bookings) // Include the bookings related to the employee
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                {
                    return NotFound("Employee not found.");
                }

                // Return the bookings associated with the employee
                var employeeBookings = employee.Bookings;
                return Ok(employeeBookings);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving bookings: {ex.Message}");
            }
        }

        [HttpPost("setEmployeesServices/{employeeId}")]
        public async Task<IActionResult> SetEmployeeServices(int employeeId, [FromBody] List<Service> services)
        {
            try
            {
                // Fetch the employee by ID
                var employee = await _bookingDbContext.Employees
                    .Include(e => e.Services) // Include associated services
                    .Include(e => e.Bookings)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                {
                    return NotFound("Employee not found.");
                }

                // Update employee services
                if (employee.Services.Any()) // Check if there are existing services
                {
                    _bookingDbContext.Services.RemoveRange(employee.Services);
                }
                employee.Bookings.Clear();
                employee.Services = services;

                // Save changes to the database
                await _bookingDbContext.SaveChangesAsync();

                return Ok("Employee services updated successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                Console.WriteLine($"Error setting employee services: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [AllowAnonymous]
        [HttpGet("getEmployeesServices/{employeeId}")]
        public async Task<IActionResult> GetEmployeesServices([FromRoute] int employeeId)
        {
            try
            {
                // Fetch the employee with the provided employeeId
                var employee = await _bookingDbContext.Employees
                    .Include(e => e.Services) // Include the associated services
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                {
                    return NotFound("Employee not found.");
                }

                // Return the services associated with the employee
                return Ok(employee.Services);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                Console.WriteLine($"Error fetching employee services: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }

  }


