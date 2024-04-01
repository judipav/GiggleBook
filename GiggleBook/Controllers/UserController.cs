using GiggleBook.Auth;
using GiggleBook.Controllers.RequestDto;
using GiggleBook.Dto;
using GiggleBook.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace GiggleBook.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly IRepository _userRepository;


    public UserController(IRepository repository)
    {
        _userRepository = repository;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest registerUserRequest)
    {
        return Ok(await _userRepository.RegisterUserAsync(new User
        {
            FirstName = registerUserRequest.FirstName,
            SecondName = registerUserRequest.SecondName,
            BirthDate = registerUserRequest.BirthDate,
            Biography = registerUserRequest.Biography,
            City = registerUserRequest.City,
            UserName = registerUserRequest.UserName,
            Sex = registerUserRequest.Sex
        }, 
        registerUserRequest.Password));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        return Ok(await _userRepository.GetUserAsync(id));
    }

    [HttpPost]
    public IActionResult Search([FromBody] FindUserRequest request)
    {
        return Ok(_userRepository.FindUser(request.FirstName, request.SecondName));
    }
}
