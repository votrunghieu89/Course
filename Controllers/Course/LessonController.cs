using Microsoft.AspNetCore.Mvc;
using E_learning.Model.Courses;
using E_learning.DTO.Course;
using E_learning.Services;
using E_learning.Repositories.Course;
using E_learning.Services.Lesson;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using E_learning.Services.Cloude;
using E_learning.Model.cloudeDB;
using E_learning.Repositories.Enrollment;
namespace E_learning.Controllers.Course
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonController : ControllerBase
    {
        private readonly ILogger<CourseController> _logger;
        private readonly ICourseRepository _courseRepo;
        private readonly GenerateID _generateID;
        private readonly CheckExsistingID _checkExsistingID;
        private readonly ConvertURL _convertURL;
        private readonly BackblazeService _backblazeService;
        private readonly RedisService _redisService;
        private readonly IEnrollmentRepository _enrollmentRepo;

        public LessonController(ILogger<CourseController> logger, ICourseRepository courseRepo, GenerateID generateID, CheckExsistingID exsistingID, ConvertURL convertURL, BackblazeService backblazeService, RedisService redisService, IEnrollmentRepository enrollmentRepository )
        {
            _logger = logger;
            _courseRepo = courseRepo;
            _generateID = generateID;
            _checkExsistingID = exsistingID;
            _convertURL = convertURL;
            _backblazeService = backblazeService;
            _redisService = redisService;
            _enrollmentRepo = enrollmentRepository;

        }
        [Authorize(Roles = "Admin,Student,Lecturer")]
        [HttpGet("GetLessonsByCourseID/{courseID}")]
        [ProducesResponseType(typeof(IEnumerable<LessonModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLessonsByCourseID(string courseID)
        {
            try
            {
                string resultKey = await _redisService.GetAsync($"lessons:course:{courseID}");
                if (!string.IsNullOrEmpty(resultKey))
                {
                    _logger.LogInformation("Returning lessons from Redis cache for course ID: {CourseID}", courseID);
                    return Ok(System.Text.Json.JsonSerializer.Deserialize<List<LessonModel>>(resultKey));
                }
                List<LessonModel> lessons = await _courseRepo.GetLessonsByCourseID(courseID);
                if (lessons == null || lessons.Count == 0)
                {
                    return NotFound("No lessons found for the specified course ID");
                }
                RedisModel redis = new RedisModel
                {
                    key = $"lessons:course:{courseID}",
                    value = System.Text.Json.JsonSerializer.Serialize(lessons),
                    expirationInSeconds = TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30) // 10 minutes and 30 seconds
                };
                await _redisService.SetAsync(redis);
                return Ok(lessons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lessons for course ID: {CourseID}", courseID);
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Lecturer")]
        [HttpDelete("DeleteLesson/{lessonID}")]
        [ProducesResponseType(statusCode: 204)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteLesson(string lessonID)
        {
            try
            {
                bool isDeleted = await _courseRepo.DeleteLessons(lessonID);
                if (!isDeleted)
                {
                    return NotFound("Lesson not found");
                }
                await _redisService.DeleteAsync($"lessons:course:{lessonID}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson with ID: {LessonID}", lessonID);
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Lecturer")]
        [HttpPost("InsertLesson")]
        [ProducesResponseType(typeof(LessonModel), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InsertLesson([FromForm] LessonDTO lesson)
        {
            try
            {
                // tạo lessonID
                string lessonID = await _checkExsistingID.GenerateUniqueIDForStringList(
                    _courseRepo.GetAllLessonsID,
                    _generateID.generateLessonID
                );
                _logger.LogInformation("🆕 Generating lesson ID: {lessonId}", lessonID);
                var model = new LessonModel(
                  lessonID,
                  lesson.lessonTitle,
                  lesson.courseID
                );
                // Thêm dữ liệu vào DB
                bool inserted = await _courseRepo.InsertLesson(model);
                if (!inserted)
                {
                    return StatusCode(500, "Failed to insert lesson");
                } else {
                    // Convert video
                    await _convertURL.UploadVideo(lesson.videoFile, lessonID);
                    string conversionResult = await _convertURL.TryConvertWithRetry(lessonID);
                    var videoPath = Path.Combine("private_videos", "videos", lessonID);
                    if (conversionResult != "Success")
                    {
                        _logger.LogError("❌ Video conversion failed for lesson {lessonId}: {error}", lessonID, conversionResult);
                        return StatusCode(500, "Video conversion failed: " + conversionResult);
                    }
                    _logger.LogInformation("✅ Video conversion successful for lesson {lessonId}", lessonID);
                    // Upload video to Backblaze
                    await _backblazeService.UploadFolderAsync(videoPath, lessonID);
                    _logger.LogInformation("🔗 Video uploaded to Backblaze for lesson {lessonId}", lessonID);
                    // Lưu URL vào Redis
                    await _redisService.DeleteAsync($"lessons:course:{lesson.courseID}"); // Xóa cache Redis nếu có
                    return Ok(new
                    {
                        message = "Lesson inserted successfully",
                        lesson = model
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ InsertLesson failed");
                return StatusCode(500, "Unexpected error occurred: " + ex.Message);
            }
        }

        [Authorize]
        [AcceptVerbs("GET", "HEAD")]
        [Route("authorize")]
        public async Task<IActionResult> AuthorizeVideoAccess()
        {
            var uri = Request.Headers["X-Original-URI"].ToString();
            Console.WriteLine("🔍 Header X-Original-URI = " + uri);

            if (string.IsNullOrEmpty(uri))
            {
                _logger.LogWarning("❌ X-Original-URI header is missing or empty.");
                return BadRequest();
            }

            var match = Regex.Match(uri, @"^/secure/videos/([^/]+)/");
            if (!match.Success)
            {
                _logger.LogWarning("❌ Invalid video URL format.");
                return BadRequest();
            }

            var lessonId = match.Groups[1].Value;
            var userId = User.FindFirst("UserID")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("❌ User ID is missing in the token.");
                return Unauthorized();
            }
            string courseID = await _courseRepo.getCourseIDbyLessonID(lessonId);
            Console.WriteLine($"🔍 Course ID for lesson {lessonId} is {courseID}");
            bool hasAccess = await _enrollmentRepo.checkBuyCourse(userId, courseID);
            Console.WriteLine($"🔍 User {userId} has access to course {courseID}: {hasAccess}");
            if (!hasAccess)
            {
                _logger.LogWarning($"❌ User {userId} does not have access to lesson {lessonId}.");
                return Forbid();
            }
            Console.WriteLine("✅ User has access to the video lesson.");
            // 🔥 Trả về rỗng nếu là HEAD (nghĩa là NGINX gọi), tránh lỗi upstream
            if (HttpContext.Request.Method == HttpMethods.Head)
            {
                _logger.LogWarning("🔍 HEAD request received, returning 200 OK without body.");
                return Ok(); // trả về 200 OK, không body
            }
            Console.WriteLine("✅ User has access to the video lesson.");
            // Nếu là GET, trả về thông báo thành công
            _logger.LogWarning("✅ Access granted for GET request.");
            return Ok(new { message = "Access granted" }); // nếu bạn test thủ công qua GET
        }

        //[HttpPost("SignedURLB2/{lessonID}")]
        //[ProducesResponseType(typeof(string), statusCode: 200)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetSignedURLB2(string lessonID)
        //{
        //    if(string.IsNullOrEmpty(lessonID))
        //    {
        //        _logger.LogWarning("❌ Lesson ID is required for generating signed URL.");
        //        return BadRequest("Lesson ID is required.");
        //    }
        //    string videoKey = $"videos/{lessonID}/index.m3u8";
        //    string videoUrlB2 = _backblazeService.GetSignedUrl(videoKey);
        //    if (string.IsNullOrEmpty(videoUrlB2))
        //    {
        //        _logger.LogError("❌ Failed to generate signed URL for lesson {lessonId}", lessonID);
        //        return NotFound("Video not found or access denied.");
        //    }
        //    _logger.LogInformation("🔗 Generated signed URL for lesson {lessonId}: {videoUrl}", lessonID, videoUrlB2);
        //    return Ok(new { videoB2Url = videoUrlB2 });
        //}
        [AllowAnonymous]
        [HttpPost("getSignedURLRedis/{lessonID}")]
        [ProducesResponseType(typeof(string), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSignedURLFromRedis(string lessonID)
        {
            string redisKey = $"video:{lessonID}";
            var redisModel = await _redisService.GetAsync(redisKey);
            if (redisModel == null)
            {
                _logger.LogWarning("❌ No signed URL found in Redis for lesson {lessonId}", lessonID);
                return NotFound("Signed URL not found in Redis.");
            }
            Console.WriteLine($"🔗 Retrieved signed URL from Redis for lesson {lessonID}: {redisModel}");

            return Ok(new { videoUrl = redisModel });
        }
        [AllowAnonymous]
        [HttpPost("getSignedURlNginx/{lessonID}")]
        [ProducesResponseType(typeof(string), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> getSignedURLNGinx(string lessonID)
        {
            string videoKey = $"http://localhost:8080/secure/videos/{lessonID}/index.m3u8";

            return Ok(new { videoUrl = videoKey });
        }
        [AllowAnonymous]
        [HttpPost("saveURlonRedis/{lessonID}")]
        [ProducesResponseType(typeof(string), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveURlonRedis(string lessonID)
        {
            string redisKey = $"video:{lessonID}";
            string videoKey = $"videos/{lessonID}/index.m3u8";
            string videoUrlB2 = _backblazeService.GetSignedUrl(videoKey);
            if (string.IsNullOrEmpty(videoUrlB2))
            {
                _logger.LogError("❌ Failed to generate signed URL for lesson {lessonId}", lessonID);
                return NotFound("Video not found or access denied.");
            }
            RedisModel newRedis = new RedisModel
           (
                redisKey,
                videoUrlB2,
                TimeSpan.FromHours(3) + Random.Shared.Next(0, 60) * TimeSpan.FromMinutes(1) 
           );
            bool isSaved = await _redisService.SetAsync(newRedis);
            if (!isSaved)
            {
                _logger.LogError("❌ Failed to save signed URL to Redis for lesson {lessonId}", lessonID);
                return StatusCode(500, "Failed to save signed URL to Redis.");
            }
            _logger.LogInformation("✅ Successfully saved signed URL to Redis for lesson {lessonId}", lessonID);
            return Ok(new { message = "Signed URL saved successfully", videoUrl = videoUrlB2 });

        }
    }
}
