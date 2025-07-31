using E_learning.Enums;
using E_learning.Model.cloudeDB;
using E_learning.Model.Users;
using E_learning.Repositories.Auth;
using E_learning.Security;
using E_learning.Services.Cloude;
using E_learning.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace E_learning.Controllers.Auth
{
    [ApiController]
    [Route("api/google")]
    public class GoogleController : ControllerBase
    {
        private readonly GoogleModel _googleConfig;
        private readonly GoogleService _googleService;
        private readonly IAuthRepository _authRepo;
        private readonly GenerateID _generateID;
        private readonly CreateAccessToken _jwtKey;
        private readonly CreateRefreshToken _refreshToken;
        private readonly RedisService _redisService;

        public GoogleController(
            IOptions<GoogleModel> googleOptions,
            GoogleService googleService,
            IAuthRepository authRepo,
            GenerateID generateID,
            CreateAccessToken jwtKey,
            CreateRefreshToken refreshToken,
            RedisService redisService)
        {
            _googleConfig = googleOptions.Value;
            _googleService = googleService;
            _authRepo = authRepo;
            _generateID = generateID;
            _jwtKey = jwtKey;
            _refreshToken = refreshToken;
            _redisService = redisService;
        }

        [HttpGet("login")]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={_googleConfig.ClientId}&redirect_uri={_googleConfig.RedirectUri}&response_type=code&scope=email%20profile&access_type=offline";
            Console.WriteLine($"Redirecting to Google OAuth: {redirectUrl}");
            return Redirect(redirectUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code)
        {
            var accessToken = await _googleService.GetAccessTokenAsync(code);
            var userInfo = await _googleService.GetGoogleUserInfoAsync(accessToken);
            Console.WriteLine($"User info retrieved: {userInfo.ToString()}");
            if (userInfo == null)
            {
                return BadRequest("Failed to retrieve user information from Google.");
            }
            var email = userInfo["email"]?.ToString();
            var name = userInfo["name"]?.ToString();
            var firstName = userInfo["given_name"]?.ToString();
            var lastName = userInfo["family_name"]?.ToString();

            var existingUser = await _authRepo.GetUserByEmailAsync(email);
            if (existingUser == null)
            {
           
                var newUser = new UserModel
                {
                    UserID = _generateID.generateUserID(),
                    Username = email,
                    Email = email,
                    FullName = name,
                    FirstName = firstName,
                    LastName = lastName,
                    Password = "", 
                    UserRole = UserRole.Student
                };
                var result = await _authRepo.AddUserAsync(newUser);
                existingUser = newUser;
            }
            var jwt = _jwtKey.GenerateJwtAccessToken(existingUser);
            var refreshToken = _refreshToken.GenerateRefreshToken();

            RedisModel redisModel = new RedisModel
            {
                key = existingUser.UserID,
                value = refreshToken,
                expirationInSeconds = TimeSpan.FromDays(7)
            };

            await _redisService.SetAsync(redisModel);

            return Ok(new
            {
                AccessToken = jwt,
                RefreshToken = refreshToken,
                User = new
                {
                    existingUser.UserID,
                    existingUser.Username,
                    existingUser.Email,
                    existingUser.FullName
                }
            });
        }
    } 
}
