using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authorization.Services.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authorization.Services;

public class AuthUserService : IAuthUserService
{
    private readonly ILogger<AuthUserService> _logger;
    private readonly IUsersRepository _repository;
    private readonly JwtSettings _jwtSettings;
    public AuthUserService(ILogger<AuthUserService> logger, IUsersRepository repository, IOptions<JwtSettings> jwtSettings)
    {
        _logger = logger;
        _repository = repository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<string> AuthenticateAsync(string username, string password)
    {
        var user = await _repository.AuthUserAsync(username, EncryptionService.EncryptPassword(password));
        
        return GenerateJwtTokenAsync(user);
    }

    private string GenerateJwtTokenAsync(User user)
    {   
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim("FullName", $"{user.SecondName} {user.FirstName}"),
            new Claim("Guid", user.Id.ToString(),ClaimTypes.NameIdentifier),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var securityToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(300),
            signingCredentials: creds);


        _logger.LogInformation($"Signed in: {user.UserName}");

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }
}

