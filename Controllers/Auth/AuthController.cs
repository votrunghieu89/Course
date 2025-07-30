using Microsoft.AspNetCore.Mvc;
using E_learning.Model.Users;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using E_learning.Services;
using Microsoft.AspNetCore.Authorization;
using E_learning.DTO.Auth;
using E_learning.Repositories.Auth;
using E_learning.Enums;
using E_learning.Security;
using E_learning.Services.Cloude;
using E_learning.Model.cloudeDB;

namespace E_learning.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _configuration;
        private readonly GenerateID _generateID;
        private readonly CreateAccessToken _jwtKey;
        private readonly CreateRefreshToken _refreshToken;
        private readonly RedisService _redisService;
        public AuthController(IAuthRepository authRepo, IConfiguration configuration, GenerateID generateID, CreateAccessToken jWT, CreateRefreshToken refreshToken, RedisService redisService)
        {
            _authRepo = authRepo;
            _configuration = configuration;
            _generateID = generateID;
            _jwtKey = jWT;
            _refreshToken = refreshToken;
            _redisService = redisService;

        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!Enum.TryParse<UserRole>(registerDto.UserRole, true, out var userRoleEnum))
            {
                return BadRequest(new { Message = "Invalid user role provided. Valid roles are: Student, Lecturer, Admin." });
            }

            var userExists = await _authRepo.CheckUsernameExistsAsync(registerDto.Username);
            if (userExists)
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            var user = new UserModel
            {
                UserID = _generateID.generateUserID(),
                Username = registerDto.Username,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                FullName = $"{registerDto.FirstName} {registerDto.LastName}",
                Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                UserRole = userRoleEnum
            };

            var result = await _authRepo.AddUserAsync(user);

            if (!result)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "User creation failed! Please check server logs." });
            }

            return Ok(new { Message = "User created successfully!" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var user = await _authRepo.GetUserByUsernameAsync(loginDto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            var Accesstoken = _jwtKey.GenerateJwtAccessToken(user);
            var RefreshToken = _refreshToken.GenerateRefreshToken();
            RedisModel redisModel = new RedisModel
            {
                key = user.UserID,
                value = RefreshToken,
                expirationInSeconds = TimeSpan.FromDays(7)

            };
            _redisService.SetAsync(redisModel);
            return Ok(new { Accesstoken, RefreshToken });
        }
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> checkRefreshToken([FromBody] RefreshTokenDTO refreshTokenDTO)
        {
            var user = await _authRepo.getUserbyID(refreshTokenDTO.UserId);
            Console.WriteLine($"User ID: {user.Email}");
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid user ID" });
            }
            var redisToken = await _redisService.GetAsync(refreshTokenDTO.UserId);
            Console.WriteLine($"Redis Token: {redisToken}");
            if (redisToken == null)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token" });
            }
            var newAccessToken = _jwtKey.GenerateJwtAccessToken(user);
            var newRefreshToken = _refreshToken.GenerateRefreshToken();
            RedisModel redisModel = new RedisModel {
                key = user.UserID,
                value = newRefreshToken,
                expirationInSeconds = TimeSpan.FromDays(7)
            };
            await _redisService.SetAsync(redisModel);
            return Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
        }

    }
}