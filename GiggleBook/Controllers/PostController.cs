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
    public IActionResult Create([FromBody] Post post) => Ok(_postsRepository.AddAsync(post));

    [HttpPut("update")]
    public IActionResult Update([FromBody] Post post) => Ok(_postsRepository.UpdateAsync(post.Id, post.Text));

    [HttpPut("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id) 
    {
        await _postsRepository.DeleteAsync(id);
        return Ok();
    } 

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await _postsRepository.GetAsync(id));

    [HttpGet("user/{id}")]
    public async Task<IActionResult> UserPosts(Guid userId) => Ok( await _postsRepository.GetUserPosts(userId));

    [HttpGet("feed/{id}")]
    public async Task<IActionResult> Feed(Guid id) => Ok(await _postsRepository.GetFeedAsync(id));
}
