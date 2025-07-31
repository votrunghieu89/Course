using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using E_learning.Model;
using E_learning.Model.Courses;
using Microsoft.Extensions.Configuration;
namespace E_learning.DAL.Course
{
    public class CoursesDAL
    {
        private readonly string _connectionString;
        private readonly ILogger<CoursesDAL> _logger;
        public CoursesDAL(IConfiguration configuration, ILogger<CoursesDAL> logger)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _logger = logger;
        }
        // lấy toàn bộ khóa học`
        public async Task<List<CoursesModel>> getAllCourse(int offset, int fetchnext)
        {
            List<CoursesModel> courses = new List<CoursesModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"SELECT CourseID, CourseName, CoursePrice, CourseDescription, AuthorID FROM Courses ORDER BY CourseID OFFSET @offset ROWS FETCH NEXT @fetchnext ROWS ONLY";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@offset", offset);
                        command.Parameters.AddWithValue("@fetchnext", fetchnext);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string courseID = reader.GetString(reader.GetOrdinal("CourseID"));
                                string courseName = reader.GetString(reader.GetOrdinal("CourseName"));
                                decimal coursePrice = reader.GetDecimal(reader.GetOrdinal("CoursePrice"));
                                string courseDescription = reader.GetString(reader.GetOrdinal("CourseDescription"));
                                string authorID = reader.GetString(reader.GetOrdinal("AuthorID"));
                                CoursesModel course = new CoursesModel(courseID, courseName, coursePrice, courseDescription, authorID);
                         
                                courses.Add(course);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving courses");
            }
            return courses;
        }
        public async Task<List<string>> getAllCourseID()
        {
            List<string> courses = new List<string>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "select CourseID from Courses";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string courseID = reader.GetString(reader.GetOrdinal("CourseID"));
                               
                                courses.Add(courseID);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving courses");
            }
            return courses;
        }
        // Thêm khóa học mới
        public async Task<bool> InsertCourse(CoursesModel course)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO Courses (CourseID, CourseName, CoursePrice,CourseDescription , AuthorID) VALUES (@CourseID, @CourseName, @CoursePrice,@CourseDescription, @AuthorID)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CourseID", course.CourseID);
                        command.Parameters.AddWithValue("@CourseName", course.CourseName);
                        command.Parameters.AddWithValue("@CoursePrice", course.CoursePrice);
                        command.Parameters.AddWithValue("@CourseDescription", course.CourseDescription);
                        command.Parameters.AddWithValue("@AuthorID", course.AuthorID);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting course");
                return false;
            }
        }
        // Xóa khóa học
        public async Task<bool> deleteCourse(string courseID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM Courses WHERE CourseID = @CourseID";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CourseID", courseID);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return false;
            }
        }
        // lấy khóa học theo ID
        public async Task<List<CoursesModel>> getCoursebyAuthorID(string authorID)
        {
            List<CoursesModel> courses = new List<CoursesModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT CourseID, CourseName, CoursePrice,CourseDescription FROM Courses WHERE AuthorID = @AuthorID";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AuthorID", authorID);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string courseID = reader.GetString(reader.GetOrdinal("CourseID"));
                                string courseName = reader.GetString(reader.GetOrdinal("CourseName"));
                                decimal coursePrice = reader.GetDecimal(reader.GetOrdinal("CoursePrice"));
                                string courseDescription = reader.GetString(reader.GetOrdinal("CourseDescription"));
                                CoursesModel course = new CoursesModel(courseID, courseName, coursePrice, courseDescription, authorID);
                                courses.Add(course);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
                _logger.LogError(ex, "Error retrieving course by ID");
            }
            return courses;
        }

        // Kiểm tra ID khóa học
        //public async Task<bool> CheckCourseIDExists(string courseID)
        //{
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(_connectionString))
        //        {
        //            await connection.OpenAsync();
        //            string query = "SELECT COUNT(*) FROM Courses WHERE CourseID = @CourseID";
        //            using (SqlCommand command = new SqlCommand(query, connection))
        //            {
        //                command.Parameters.AddWithValue("@CourseID", courseID);
        //                int count = (int)await command.ExecuteScalarAsync();
        //                return count > 0;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error checking if course ID exists");
        //        return false;
        //    }
        //}
    }
}
