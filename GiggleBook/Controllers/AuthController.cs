using GiggleBook.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GiggleBook.Controllers.RequestDto;

namespace GiggleBook.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationManager _authenticationManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationManager authenticationManager, ILogger<AuthController> logger)
    {
        this._authenticationManager = authenticationManager;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authenticationManager.ValidateCredentials(request.UserName, request.Password);
        
        if (user == null)
        {
            return Unauthorized("Неверный логин или пароль");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("FullName", string.Concat(user.SecondName, " ", user.FirstName)),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {1} logged in at {2}.", user.UserName, DateTime.UtcNow);

        return Ok(user);
    }

    [HttpGet]
    public async Task Logout()
    {
        _logger.LogInformation("User {1} logged out at {2}.", HttpContext.User.Claims.First(c => c.Type.Equals(ClaimTypes.Name)).Value, DateTime.UtcNow);
        
        await HttpContext.SignOutAsync();
    }
}
