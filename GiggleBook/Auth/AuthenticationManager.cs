using GiggleBook.Data;
using GiggleBook.Dto;
using GiggleBook.Services;

namespace GiggleBook.Auth;

public class AuthenticationManager : IAuthenticationManager
{
    private readonly UsersRepository _repository;

    public AuthenticationManager(UsersRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> ValidateCredentials(string username, string password) => await _repository.AuthUserAsync(username, EncryptionService.EncryptPassword(password));
}
