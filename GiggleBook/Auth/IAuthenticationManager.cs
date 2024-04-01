using GiggleBook.Dto;

namespace GiggleBook.Auth;

public interface IAuthenticationManager 
{
    Task<User> ValidateCredentials(string username, string password);
    //User ValidatePrincipal(string token);
}