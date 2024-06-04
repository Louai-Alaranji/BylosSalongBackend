using BookingApp_Backend.Helpers;
using System.Net;
using System.Net.Mail;

public class EmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string message)
    {
        string mail = "alaranjisistersbeautycenter@gmail.com";
        string password = "rfpa exes typo qsod";

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(mail, password)
        };

        return client.SendMailAsync(
            new MailMessage(from: mail,
                            to: email,
                            subject: subject,
                            message
                            ));
    }
}