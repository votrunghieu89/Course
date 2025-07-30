using E_learning.Enums;
using E_learning.Model.Users;
using E_learning.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace E_learning.DAL.Auth
{
    public class AuthDAL
    {
        private readonly string _connectionString;
        private readonly ILogger<AuthDAL> _logger;

        public AuthDAL(IConfiguration configuration, ILogger<AuthDAL> logger)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _logger = logger;
        }

        public async Task<UserModel> GetUserByUsername(string username)
        {
            UserModel user = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Users WHERE Username = @Username";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Enum.TryParse<UserRole>(reader.GetString(reader.GetOrdinal("UserRole")), true, out var role);
                                user = new UserModel
                                {
                                    UserID = reader.GetString(reader.GetOrdinal("UserID")),
                                    Username = reader.GetString(reader.GetOrdinal("Username")),
                                    Password = reader.GetString(reader.GetOrdinal("Password")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    FullName = $"{reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}",
                                    UserRole = role
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
            }
            return user;
        }
        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        int count = (int)await command.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username exists: {Username}", username);
                return true;
            }
        }

        public async Task AddUserAsync(UserModel user, SqlConnection connection, SqlTransaction transaction)
        {
            string query = @"INSERT INTO Users (UserID, Username, Password, Email, FirstName, LastName, FullName, UserRole) 
                             VALUES (@UserID, @Username, @Password, @Email, @FirstName, @LastName, @FullName, @UserRole)";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserID", user.UserID);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@FullName", user.FullName);
                command.Parameters.AddWithValue("@UserRole", user.UserRole.ToString());
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddStudentAsync(string userID, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "INSERT INTO Students (UserID) VALUES (@UserID)";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserID", userID);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddLecturerAsync(string userID, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "INSERT INTO Lecturers (UserID) VALUES (@UserID)";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserID", userID);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task AddAdminAsync(string userID, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "INSERT INTO Admin (UserID) VALUES (@UserID)";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserID", userID);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<UserModel> getUserbyID( string userID)
        {
            try
            {
                UserModel model = new UserModel();
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT Email, UserRole FROM Users WHERE UserID = @UserID";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userID);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Enum.TryParse<UserRole>(reader.GetString(reader.GetOrdinal("UserRole")), true, out var role);
                                model = new UserModel
                                {
                                    UserID = userID,
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    UserRole = role
                                };
                            }
                        }
                    }
                }
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserID}", userID);
                return null;
            }
        }

        public async Task<UserModel> GetUserByEmailAsync(string email)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Users WHERE Email = @Email";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Enum.TryParse<UserRole>(reader.GetString(reader.GetOrdinal("UserRole")), true, out var role);
                                var model = new UserModel
                                {
                                    UserID = reader.GetString(reader.GetOrdinal("UserID")),
                                    Username = reader.GetString(reader.GetOrdinal("Username")),
                                    Password = reader.GetString(reader.GetOrdinal("Password")),
                                    Email = email,
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    FullName = $"{reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}",
                                    UserRole = role
                                };
                                return model;
                            }
                            else
                            {
                               
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                return null;
            }
        }
    }
}