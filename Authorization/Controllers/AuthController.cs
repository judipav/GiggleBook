using Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers;
public record UserLoginModel(string Username, string Password);

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly IAuthUserService _authService;
    public AuthController(IAuthUserService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] UserLoginModel model) 
    {
        var token = await _authService.AuthenticateAsync(model.Username, model.Password);
        if (token == null)
        {
            return Unauthorized();
        }

        HttpContext.Response.Cookies.Append("GiggleBookAuth", token);

        return Ok(new { message = "Authentication successful" });
    }

    [HttpGet]
    public void Logout()
    {
        HttpContext.Response.Cookies.Delete("GiggleBookAuth");
    }
}

