using System.Security.Claims;
using Chirper.Auth;
using Chirper.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chirper.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class DialogController : ControllerBase
{
    private readonly IDialogRepository _repository;
    public DialogController(IDialogRepository dialogRepository)
    {
        _repository = dialogRepository;
    }

    [HttpPost("{user_id}/send")]
    public async Task<IActionResult> Send(Guid user_id, [FromBody] string text, CancellationToken token) 
    {
        string ident = HttpContext.User.Claims.First(c => c.Type == "Guid").Value;
        var myId = Guid.Parse(ident);
        await _repository.Send(new Chirp(myId, user_id, text, DateTime.UtcNow), token);
        return Ok();
    }

    [HttpGet("{user_id}/list/{skip}")]
    public IAsyncEnumerable<Chirp> List(Guid user_id, int skip, CancellationToken token) 
    {
        var myId = Guid.Parse(HttpContext.User.Claims.First(c => c.Type == "Guid").Value);
        return _repository.List(myId, user_id, skip, token);
    }
}

