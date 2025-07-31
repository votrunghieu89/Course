using E_learning.DTO.Enrollment;
using E_learning.Model.Courses;
using E_learning.Model.Enrollment;
using E_learning.Repositories.Enrollment;
using E_learning.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_learning.Controllers.Enrollment
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class EnrollmentController : ControllerBase
    {
        private readonly ILogger<EnrollmentController> _logger;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly GenerateID _generateID;
        private readonly CheckExsistingID _checkExsistingID;
        public EnrollmentController(ILogger<EnrollmentController> logger, IEnrollmentRepository enrollmentRepo, GenerateID generateID, CheckExsistingID checkExsistingID)
        {
            _logger = logger;
            _enrollmentRepo = enrollmentRepo;
            _generateID = generateID;
            _checkExsistingID = checkExsistingID;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllEnrollments")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEnrollments(int offset, int fetchnext)
        {
            try
            {
                var enrollments = await _enrollmentRepo.GetAllEnrollments(offset, fetchnext);
                if (enrollments == null || !enrollments.Any())
                {
                    return NotFound("No enrollments found");
                }
                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all enrollments");
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Admin,Student,Lecturer")]
        [HttpPost("InsertEnrollment")]
        [ProducesResponseType(typeof(EnrollmentModel), statusCode: 201)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InsertEnrollment([FromBody] EnrollmentDTO enrollment)
        {
            if (enrollment == null)
            {
                return BadRequest("Enrollment data is null");
            }
            try
            {
                string newID = await _checkExsistingID.GenerateUniqueIDForStringList(_enrollmentRepo.GetAllEnrollmentsID, _generateID.GenerateEnrollmentID);
                Console.WriteLine("New Enrollment ID: " + newID);
                EnrollmentModel enrollmen = new EnrollmentModel(
                    newID,
                    enrollment.UserID,
                    enrollment.CourseID
                    );
                bool isInsert = await _enrollmentRepo.InsertEnrollment(enrollmen);
                if (!isInsert)
                {
                    return BadRequest("Enrollment could not be inserted");
                }
                return CreatedAtAction(nameof(GetAllEnrollments), new { id = newID }, enrollmen);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting enrollment");
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize(Roles = "Admin,Student,Lecturer")]
        [HttpGet("GetEnrollmentsByUserID/{userID}")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEnrollmentsByUserID(string userID)
        {
            try
            {
                var enrollments = await _enrollmentRepo.getEnrollbyUserID(userID);
                if (enrollments == null || !enrollments.Any())
                {
                    return NotFound("No enrollments found for the specified user ID");
                }
                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enrollments for user ID: {UserID}", userID);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
