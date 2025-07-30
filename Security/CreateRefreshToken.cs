using System.Security.Cryptography;

namespace E_learning.Security
{
    public class CreateRefreshToken
    {
        public  string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
