
using Microsoft.AspNetCore.Http.HttpResults;

namespace BookingApp_Backend.Helpers
{
    public class PasswordHashing
    {
        public static string HashPassword(string password)
        {
            //return BCryptNet.HashPassword(password);
            string passwodHash = BCrypt.Net.BCrypt.HashPassword(password);
            return passwodHash;
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            
        }
    }
}
