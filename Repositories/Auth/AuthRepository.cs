using E_learning.DAL.Auth;
using E_learning.Enums;
using E_learning.Model.Users;
using E_learning.Enums;
using E_learning.Services;
using Microsoft.Data.SqlClient;

namespace E_learning.Repositories.Auth
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AuthDAL _authDAL;
        private readonly IConfiguration _configuration;
        private readonly GenerateID _generateID;

        public AuthRepository(AuthDAL authDAL, IConfiguration configuration, GenerateID generateID)
        {
            _authDAL = authDAL;
            _configuration = configuration;
            _generateID = generateID;
        }

        public async Task<bool> AddUserAsync(UserModel user)
        {
            string connectionString = _configuration.GetConnectionString("SqlServerConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await _authDAL.AddUserAsync(user, connection, transaction);

                        switch (user.UserRole)
                        {
                            case UserRole.Student:
                                string studentID = _generateID.generateStudentID();
                                await _authDAL.AddStudentAsync(user.UserID, connection, transaction);
                                break;
                            case UserRole.Lecturer:
                                string lecturerID = _generateID.generateLecturerID();
                                await _authDAL.AddLecturerAsync(user.UserID, connection, transaction);
                                break;
                            case UserRole.Admin:
                                string adminID = _generateID.generateAdminID();
                                await _authDAL.AddAdminAsync(user.UserID, connection, transaction);
                                break;
                            default:
                                throw new Exception($"Invalid user role for database insertion: {user.UserRole}");
                        }
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                }
            }
        }

        public Task<bool> CheckUsernameExistsAsync(string username) => _authDAL.CheckUsernameExistsAsync(username);

        public Task<UserModel> GetUserByUsernameAsync(string username) => _authDAL.GetUserByUsername(username);

        public Task<UserModel> getUserbyID(string userID)
        {
            return _authDAL.getUserbyID(userID);
        }
        public Task<UserModel> GetUserByEmailAsync(string email)
        {
            return _authDAL.GetUserByEmailAsync(email);
        }
    }
}