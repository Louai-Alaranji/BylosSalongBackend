namespace BookingApp_Backend.Helpers
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);

    }
}
