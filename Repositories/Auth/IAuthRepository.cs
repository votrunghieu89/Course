using E_learning.Model.Users;

namespace E_learning.Repositories.Auth
{
    public interface IAuthRepository
    {
        Task<UserModel> GetUserByUsernameAsync(string username);
        Task<bool> CheckUsernameExistsAsync(string username);
        Task<bool> AddUserAsync(UserModel user);
        Task<UserModel> getUserbyID(string userID);
        Task<UserModel> GetUserByEmailAsync(string email);
    }
}
