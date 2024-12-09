using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GiggleBook.Dto;
using GiggleBook.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GiggleBook.Auth;

public class AuthenticationManager : IAuthenticationManager
{
    private readonly UsersRepository _repository;
    private readonly ILogger<AuthenticationManager> _logger;

    public AuthenticationManager(UsersRepository repository, ILogger<AuthenticationManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<User> AuthenticateAsync(string username, string password, HttpContext httpContext) 
    {
        var user = await _repository.AuthUserAsync(username, EncryptionService.EncryptPassword(password));

        return await GenerateCookies(user, httpContext);
    } 

    private async Task<User> GenerateCookies(User user, HttpContext httpContext)
    {   
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim("FullName", $"{user.SecondName} {user.FirstName}"),
            new Claim("Guid", user.Id.ToString(),ClaimTypes.NameIdentifier),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = true
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {1} logged in at {2}.", user.UserName, DateTime.UtcNow);

        return user;
    }
}
