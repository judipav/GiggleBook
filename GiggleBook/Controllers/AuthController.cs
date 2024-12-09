using GiggleBook.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using GiggleBook.Controllers.RequestDto;

namespace GiggleBook.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationManager _authenticationManager;

    public AuthController(IAuthenticationManager authenticationManager)
    {
        _authenticationManager = authenticationManager;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authenticationManager.AuthenticateAsync(request.UserName, request.Password, HttpContext);
        if (user == null)
        {
            return Unauthorized();
        }
        return Ok(user);
    }

    [HttpGet]
    public async Task Logout()
    {
        await HttpContext.SignOutAsync();
    }
}
