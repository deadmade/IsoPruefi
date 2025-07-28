using Database.EntityFramework.Models;
using Rest_API.Models;

namespace Rest_API.Services.Auth;

public interface IAuthenticationService
{
    Task Register(Register input);
    Task<JwtToken> Login(Login input);
    Task<JwtToken> RefreshToken(JwtToken tokenModel);
    Task<List<ApiUser>> GetUserInformations();
    Task<ApiUser?> GetUserById(string userId);
    Task ChangePassword(ApiUser user, string currentPassword, string newPassword);
    Task ChangeUser(ApiUser user);
    Task<bool> DeleteUser(ApiUser user);
}