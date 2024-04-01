using GiggleBook.Dto;

namespace GiggleBook.Interfaces;

public interface IRepository
{
    Task<User> AuthUserAsync(string name, string token);
    Task<bool> RegisterUserAsync(User user, string password);
    Task<User> GetUserAsync(string id);
    UserDto[] FindUser(string firstName, string secondName);
}
