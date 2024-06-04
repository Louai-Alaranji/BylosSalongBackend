using BookingApp_Backend.Data;
using BookingApp_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingApp_Backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BookingDbContext _bookingDbContext;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    public AuthController(IConfiguration configuration, BookingDbContext context, IWebHostEnvironment hostEnvironment )
    {
        _configuration = configuration;
        _bookingDbContext = context;
        this._hostEnvironment = hostEnvironment;
    }

    [HttpPost("/api/Auth/login")]
    public async Task<IActionResult> Login(Login loginData)
    {
        var employee = await _bookingDbContext.Employees
            .FirstOrDefaultAsync(e => e.Email == loginData.Email);

        var passwordHash = PasswordHashing.VerifyPassword(loginData.Password, employee.PasswordHash);

        if (employee == null || !passwordHash)
        {
            return BadRequest("Invalid email or password.");
        }

        var tokenGenerator = new TokenGenerator(_configuration);
        var token = tokenGenerator.GenerateToken(employee);

        return Ok(new { token = token, employeeId = employee.Id });
    }

    [Authorize]
    [HttpPost("/api/Auth/register")]
    public async Task<IActionResult> Register(Register registerData)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.Values.SelectMany(v => v.Errors));
        }

        // Check for existing user with the same email
        var existingUser = await _bookingDbContext.Employees.FirstOrDefaultAsync(e => e.Email == registerData.Email);
        if (existingUser != null)
        {
            return BadRequest("Email already exists.");
        }

        

        // Hash the password before storing
        var passwordHash = PasswordHashing.HashPassword(registerData.Password);

        var newEmployee = new Employee
        {
            Email = registerData.Email,
            Phone = registerData.Phone,
            Name = registerData.Name,
            PasswordHash = passwordHash,
            Job = registerData.Job,
            ImageName = registerData.ImageName,
        };

        try
        {

            if (registerData.ImageFile != null)
            {
                newEmployee.ImageName = await SaveImage(registerData.ImageFile);
            }


            _bookingDbContext.Employees.Add(newEmployee);
            await _bookingDbContext.SaveChangesAsync();
            return new CreatedResult(Url.Action("GetEmployee", new { employeeId = newEmployee.Id }), newEmployee );
        } 
        catch (Exception ex)
        {
            return BadRequest("An error occurred during registration. Please try again later." + ex);
        }
    }


    [HttpGet("GetEmployee/{employeeId}")]
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

    [NonAction]
    public async Task<string> SaveImage(IFormFile imageFile)
    {   // we get the first 10 chars in a filename
        string imageName = new String(Path.GetFileNameWithoutExtension(imageFile.FileName).Take(10).ToArray()).Replace(" ", "-");

        imageName = imageName + DateTime.Now.ToString("yymmssff")+Path.GetExtension(imageFile.FileName);
        var imagePath = Path.Combine(_hostEnvironment.ContentRootPath, "Images", imageName);
        using (var fileStream = new FileStream(imagePath, FileMode.Create))
        {
             await imageFile.CopyToAsync(fileStream);
        }
        return imageName;
    }

    


}
