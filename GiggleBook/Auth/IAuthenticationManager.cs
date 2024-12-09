using GiggleBook.Dto;

namespace GiggleBook.Auth;

public interface IAuthenticationManager 
{
    Task<User> AuthenticateAsync(string username, string password, HttpContext httpContext);
}