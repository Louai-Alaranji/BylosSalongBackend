using BookingApp_Backend.Data;
using BookingApp_Backend.Helpers;
using BookingApp_Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BookingApp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BookingDbContext _bookingDbContext;
        private readonly IEmailSender _emailSender;
        public UserController(BookingDbContext context, IEmailSender emailService)
        {
            _bookingDbContext = context;
            _emailSender = emailService;
        }

        [HttpGet("GetAllEmployees")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAllEmployees()
        {
            try
            {
                var employees = await _bookingDbContext.Employees.ToListAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving employees: {ex.Message}");
            }
        }


        // AVAIALBE HOURS FOR SERVICES ARE GETTING FLIBBED. THEY ARE ONLY VISIBLE ON THE FIRST SERVICE. 
        [HttpPost("appointments/book")]
        public async Task<IActionResult> BookAppointment([FromBody] BookingRequest bookingRequest)
        {
            try
            {
                // Check if the booking request contains valid data
                if (bookingRequest == null || bookingRequest.EmployeeId <= 0 || bookingRequest.Date == default || bookingRequest.StartTime == default(TimeSpan))
                {
                    return BadRequest("Invalid booking request.");
                }

                // Validate email
                if (string.IsNullOrWhiteSpace(bookingRequest.Email) || !IsValidEmail(bookingRequest.Email))
                {
                    return BadRequest("Invalid email address.");
                }

                // Validate phone (optional)
                if (string.IsNullOrWhiteSpace(bookingRequest.Phone))
                {
                    return BadRequest("Phone number is required.");
                }

                // Check if the employee exists
                var employee = await _bookingDbContext.Employees
                  .Include(e => e.Services) // Include services
                  .ThenInclude(s => s.AvailableHours) // Include available hours
                  .FirstOrDefaultAsync(e => e.Id == bookingRequest.EmployeeId);

                if (employee == null)
                {
                    return NotFound("Employee not found.");
                }

                var existingBooking = await _bookingDbContext.BookingRequests
                .FirstOrDefaultAsync(b => b.EmployeeId == bookingRequest.EmployeeId &&
                                      b.Email == bookingRequest.Email &&
                                      b.Date >= DateTime.Now);

                if (existingBooking != null)
                {
                    return BadRequest("You already have a booking with this employee. Please wait until your current booking has passed.");
                }

                string employeeName = employee.Name;

                // Get all available hours for the selected date across all services
                var availableHours = employee.Services
                  .SelectMany(s => s.AvailableHours)
                  .Where(ah => ah.Date.Date == bookingRequest.Date.Date && ah.IsAvailable)
                  .ToList();

                // Check if the requested hour is available in any service
                var matchingAvailableHour = availableHours.FirstOrDefault(ah => ah.StartTime == bookingRequest.StartTime);

                if (matchingAvailableHour == null || !matchingAvailableHour.IsAvailable)
                {
                    return BadRequest("Selected time slot is not available.");
                }

                // Check if the duration of the service exceeds the available time slot
                var service = employee.Services.FirstOrDefault(s => s.Id == bookingRequest.ServiceId);

                if (service == null)
                {
                    return BadRequest("Service not found.");
                }

                var serviceDuration = TimeSpan.FromMinutes(service.DurationInMinutes); // 00:45:00

                var startTime = bookingRequest.StartTime;  //09:45:00
                int totalMinutes = (int)(startTime.TotalMinutes + serviceDuration.TotalMinutes);
                var endTime = startTime.Add(serviceDuration - TimeSpan.FromMinutes(15)); // 10:30:00

                var availableHoursForService = availableHours
                .Where(ah => ah.ServiceId == bookingRequest.ServiceId)
                .OrderBy(ah => ah.StartTime)
                .ToList();


                var startTimeIndex = availableHoursForService.FindIndex(ah => ah.StartTime == startTime);
                var endTimeIndex = availableHoursForService.FindIndex(ah => ah.StartTime == endTime);


                if (endTimeIndex == -1)
                {
                    // If the end time doesn't match exactly, find the closest available hour
                    var closestAvailableEndTime = availableHoursForService
                        .Where(ah => ah.StartTime > endTime)
                        .OrderBy(ah => ah.StartTime)
                        .FirstOrDefault();

                    if (closestAvailableEndTime == null)
                    {
                        // No available hours after the start time, service duration exceeds available time
                        return BadRequest("Service duration is longer than available time");
                    }

                    // Calculate the actual end time based on the closest available end time
                    endTime = closestAvailableEndTime.StartTime;
                }


                // Calculate all time slots to be marked unavailable based on service duration
                var overlappingAvailableHours = availableHours.Where(ah =>
                ah.StartTime >= startTime && ah.StartTime <= endTime);

                // Mark overlapping slots unavailable across all services
                foreach (var availableHour in overlappingAvailableHours)
                {
                    availableHour.IsAvailable = false;
                }

                employee.Bookings ??= new List<BookingRequest>(); // Ensure the Bookings collection is initialized
                employee.Bookings.Add(bookingRequest);
                await _bookingDbContext.SaveChangesAsync();

                string confirmationMessage = $"Your appointment has been successfully booked with {employeeName}\n " +
                                        $"Service: {service.Name} \n Duration: {service.DurationInMinutes} minutes\n " +
                                                $"Date: {bookingRequest.Date.Date.ToString("yyyy-MM-dd")}.\n Hour: {bookingRequest.StartTime}";
                await _emailSender.SendEmailAsync(bookingRequest.Email, "Booking Confirmation", confirmationMessage);

                return Ok("Appointment booked successfully.");
                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while booking appointment: {ex.Message}");
            }
        }


        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest messageRequest)
        {
            try
            {
                // Construct the message body
                string emailBody = $"Name: {messageRequest.Name}\n Email: {messageRequest.Email}\n Message: {messageRequest.Message}";

                // Send the email using the email sender service
                await _emailSender.SendEmailAsync($"leoamandaofficial@gmail.com", $"{messageRequest.Subject}", $"{emailBody}");

                return Ok("Message sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while sending the message: {ex.Message}");
            }
        }


        [HttpPost("appointments/send-verification-code")]
        public async Task<IActionResult> SendVerificationCode([FromBody] BookingRequest bookingRequest)
        {
            try
            {
                // Validate email
                if (string.IsNullOrWhiteSpace(bookingRequest.Email) || !IsValidEmail(bookingRequest.Email))
                {
                    return BadRequest("Invalid email address.");
                }

                // Generate verification code
                string verificationCode = GenerateVerificationCode();

                // Save verification code in the database
                var emailVerification = new EmailVerification
                {
                    Email = bookingRequest.Email,
                    Code = verificationCode
                };
                _bookingDbContext.EmailVerifications.Add(emailVerification);
                await _bookingDbContext.SaveChangesAsync();

                // Send verification code via email
                string emailBody = $"Your verification code is: {verificationCode}.";
                await _emailSender.SendEmailAsync(bookingRequest.Email, "Booking Verification Code", emailBody);

                return Ok("Verification code sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while sending the verification code: {ex.Message}");
            }
        }

        [HttpPost("appointments/verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] EmailVerification verificationRequest)
        {
            try
            {
                // Retrieve the verification request from the database based on the email address
                // Retrieve the latest verification request from the database based on the email address
                var emailVerification = await _bookingDbContext.EmailVerifications
                    .OrderByDescending(c => c.Id)  // Assuming Id is a unique identifier for the verification request and represents its creation order
                    .FirstOrDefaultAsync(c => c.Email == verificationRequest.Email);

                // Check if the verification request exists and if the verification code matches
                if (emailVerification == null)
                {
                    return BadRequest("Invalid verification code.");
                }

                if (emailVerification.Code != verificationRequest.Code)
                {
                    return BadRequest("Invalid verification code.");
                }

                // If the verification is successful, you can perform any additional actions here

                // For example, you can delete the verification request from the database
                _bookingDbContext.EmailVerifications.Remove(emailVerification);
                await _bookingDbContext.SaveChangesAsync();

                // Return success message
                return Ok("Verification code verified successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while verifying verification code: {ex.Message}");
            }
        }

        
        private string GenerateVerificationCode()
        {
            // Generate a random 4-digit code
            Random random = new Random();
            return random.Next(1000, 9999).ToString();
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /*
        [HttpDelete("appointments/delete-all-verification-codes")]
        public async Task<IActionResult> DeleteAllVerificationCodes()
        {
            try
            {
                // Retrieve all verification codes from the database
                var verificationCodes = await _bookingDbContext.EmailVerifications.ToListAsync();

                // Check if there are any verification codes to delete
                if (verificationCodes == null || verificationCodes.Count == 0)
                {
                    return Ok("No verification codes found to delete.");
                }

                // Remove all verification codes from the database
                _bookingDbContext.EmailVerifications.RemoveRange(verificationCodes);
                await _bookingDbContext.SaveChangesAsync();

                // Return success message
                return Ok("All verification codes deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting verification codes: {ex.Message}");
            }
        }*/
    }

}
