using BookingApp_Backend.Data;
using BookingApp_Backend.Helpers;
using BookingApp_Backend.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ADD THIS FOR ENABLE IMAGE UPLOAD
IWebHostEnvironment env = builder.Environment;

builder.Services.AddDbContext<BookingDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddControllers();

// automatic background service that run every x time
builder.Services.AddHangfire(opt =>
{
    opt.UseMemoryStorage()
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseDefaultTypeSerializer();
});

builder.Services.AddHangfireServer();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
                
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
                    //ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    //ValidAudience = builder.Configuration["Jwt:Audience"]
                };
            });



var app = builder.Build();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();


app.UseHangfireDashboard();

string imagePath = Path.Combine(env.ContentRootPath, "Images");
if (!Directory.Exists(imagePath))
{
    Directory.CreateDirectory(imagePath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagePath),
    RequestPath = "/Images"
});

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<BookingDbContext>("DeleteOldBookingsAndAvailableHours",
     x => x.DeleteOldBookingsAndAvailableHours(),
    "59 23 * * *"
    );

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

    if (!context.Employees.Any(e => e.Email == "admin@admin.com"))
    {
        var passwordHash = PasswordHashing.HashPassword("Admin@123");
        var adminUser = new Employee
        {
            Email = "admin@admin.com",
            PasswordHash = passwordHash
        };

        context.Employees.Add(adminUser);
        context.SaveChanges();
    }
}
// Configure the HTTP request pipeline.

app.MapControllers();

app.MapGet("/", () => "Hello World, I am here!");
app.Run();
