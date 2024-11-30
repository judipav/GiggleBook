using GiggleBook.Auth;
using GiggleBook.Dto;
using GiggleBook.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GiggleBook.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class PostController : ControllerBase
{
    private readonly IPostsRepository _postsRepository;
    public PostController(IPostsRepository postsRepository)
    {
        _postsRepository = postsRepository;
    }

    [HttpPost("create")]
    public IActionResult Create([FromBody] Post post, CancellationToken token) => Ok(_postsRepository.AddAsync(post, token));

    [HttpPut("update")]
    public IActionResult Update([FromBody] Post post, CancellationToken token) => Ok(_postsRepository.UpdateAsync(post.Id, post.Text, token));

    [HttpPut("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) 
    {
        await _postsRepository.DeleteAsync(id, token);
        return Ok();
    } 

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await _postsRepository.GetAsync(id, token));

    [HttpGet("user/{id}")]
    public async Task<IActionResult> UserPosts(Guid userId, CancellationToken token) => Ok( await _postsRepository.GetUserPosts(userId, token));

    [HttpGet("feed/{id}")]
    public IAsyncEnumerable<Post> Feed(Guid id, CancellationToken token) => _postsRepository.GetFeedAsync(id, token);
}
