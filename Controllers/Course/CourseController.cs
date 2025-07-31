using Microsoft.AspNetCore.Mvc;
using E_learning.Model.Courses;
using E_learning.DTO.Course;
using E_learning.Services;
using E_learning.Repositories.Course;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using E_learning.Services.Cloude;
using E_learning.Model.cloudeDB;
namespace E_learning.Controllers.Course
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseController : Controller
    {
        private readonly ILogger<CourseController> _logger;
        private readonly ICourseRepository _courseRepo;
        private readonly GenerateID _generateID;
        private readonly RedisService _redisService;
        private readonly CheckExsistingID _checkExsistingID;
        public CourseController(ILogger<CourseController> logger, ICourseRepository courseRepo, GenerateID generateID, CheckExsistingID exsistingID, RedisService redisService)
        {
            _logger = logger;
            _courseRepo = courseRepo;
            _generateID = generateID;
            _checkExsistingID = exsistingID;
            _redisService = redisService;
        }
        [Authorize(Roles = "Admin,Student,Lecturer")]
        [HttpGet("GetAllCourses")]
        [ProducesResponseType(typeof(IEnumerable<CoursesModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllCourses(int offset, int fetchnext)
        {
            try
            {
                string resultRedis = await _redisService.GetAsync($"course:offset:{offset}:limit:{fetchnext}");
                if(!string.IsNullOrEmpty(resultRedis))
                {
                    _logger.LogInformation("Returning courses from Redis cache for offset: {Offset}, limit: {Limit}", offset, fetchnext);
                    return Ok(System.Text.Json.JsonSerializer.Deserialize<List<CoursesModel>>(resultRedis));
                }
                List<CoursesModel> courses = await _courseRepo.GetAllCourses(offset, fetchnext);
                if (courses == null || courses.Count == 0)
                {
                    return NotFound("No courses found");
                }
                RedisModel redis = new RedisModel {
                    key = $"course:offset:{offset}:limit:{fetchnext}",
                    value = System.Text.Json.JsonSerializer.Serialize(courses),
                    expirationInSeconds = TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30) // 10 minutes and 30 seconds
                    };
                await _redisService.SetAsync(redis);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all courses");
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Lecturer")]
        [HttpPost("InsertCourse")]
        [ProducesResponseType(typeof(CoursesModel), statusCode: 201)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InsertCourse([FromBody] CourseDTO course)
        {
            if (course == null)
            {
                return BadRequest("Course data is null");
            }
            try
            {
                string newID = await _checkExsistingID.GenerateUniqueIDForStringList(
                    _courseRepo.GetAllCoursesID,
                    _generateID.generateCourseID
                );
                CoursesModel courseModel = new CoursesModel(
                    newID,
                    course.CourseName,
                    course.CoursePrice,
                    course.CourseDescription,
                    course.Author
                );
                bool isInserted = await _courseRepo.InsertCourse(courseModel);
                if (isInserted)
                {
                    await _redisService.DeleteAsync($"course:authorID:{course.Author}");
                    return Ok(new { Message = "Course inserted successfully." });
                }
                else
                {
                    return BadRequest("Failed to insert course");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting course");
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Lecturer")]
        [HttpDelete("DeleteCourse/{courseID}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCourse(string courseID)
        {
            if (string.IsNullOrEmpty(courseID))
            {
                return BadRequest("Course ID is null or empty");
            }
            try
            {
                bool isDeleted = await _courseRepo.DeleteCourse(courseID);
                if (isDeleted)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound("Course not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course with ID: {CourseID}", courseID);
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Lecturer")]
        [HttpGet("GetCourseByID/{authorID}")]
        [ProducesResponseType(typeof(CoursesModel), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCourseByID(string authorID)
        {
            if (string.IsNullOrEmpty(authorID))
            {
                return BadRequest("Course ID is null or empty");
            }
            try
            {
                string resultRedis = await _redisService.GetAsync($"course:authorID:{authorID}");
                if (!string.IsNullOrEmpty(resultRedis))
                {
                    _logger.LogInformation("Returning course from Redis cache for author ID: {AuthorID}", authorID);
                    return Ok(System.Text.Json.JsonSerializer.Deserialize<List<CoursesModel>>(resultRedis));
                }
                List<CoursesModel> courses = await _courseRepo.getCoursebyAuthorID(authorID);
                if (courses == null)
                {
                    return NotFound("Course not found");
                }
                Console.WriteLine($"Courses retrieved: {courses.Count}");
                RedisModel redis = new RedisModel
                {
                    key = $"course:authorID:{authorID}",
                    value = System.Text.Json.JsonSerializer.Serialize(courses),
                    expirationInSeconds = TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30) // 10 minutes and 30 seconds
                };
                await _redisService.SetAsync(redis);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving course with ID: {CourseID}", authorID);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
