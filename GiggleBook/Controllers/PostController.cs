using GiggleBook.Auth;
using GiggleBook.Dto;

using Microsoft.AspNetCore.Mvc;

namespace GiggleBook.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class PostController : ControllerBase
{
    [HttpPost("create")]
    public IActionResult Create([FromBody] string[] text) => Ok();

    [HttpPut("update")]
    public IActionResult Update([FromBody] Post post) => Ok();

    [HttpPut("delete/{id}")]
    public IActionResult Delete(Guid id) => Ok();

    [HttpGet("get/{id}")]
    public IActionResult Get(Guid id) => Ok();
}
