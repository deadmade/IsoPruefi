using Database.EntityFramework.Models;

namespace Rest_API.Services.User;

public interface IUserService
{
    Task<List<ApiUser>> GetUserInformations();
    Task<ApiUser?> GetUserById(string userId);
    Task ChangePassword(ApiUser user, string currentPassword, string newPassword);
    Task ChangeUser(ApiUser user);
    Task<bool> DeleteUser(ApiUser user);
}