namespace Chirper.Services;

public class JwtSettings
{
    public const string PathConfiguration = "Jwt"; 
    public required string Key { get; set; }
    public required string Audience { get; set; }
    public required string Issuer { get; set; }
}

