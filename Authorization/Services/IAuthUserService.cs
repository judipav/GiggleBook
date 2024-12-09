namespace Authorization.Services;

public interface IAuthUserService
{
    Task<string> AuthenticateAsync(string username, string password);
}
