using GiggleBook.Auth;
using Microsoft.AspNetCore.Mvc;

namespace GiggleBook.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class FriendController : ControllerBase
{
    [HttpPut("{user_id}")]
    public IActionResult Set(string user_id)
    {
        return Ok();
    }

    [HttpPut("{user_id}")]
    public IActionResult Delete(string user_id)
    {
        return Ok();
    }
}
