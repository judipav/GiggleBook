namespace GiggleBook.Services;

public class RedisConfig 
{
    public const string PathConfiguration = "Redis";
    public required string ConnectionString { get; set; }
}