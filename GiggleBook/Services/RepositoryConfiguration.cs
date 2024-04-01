namespace GiggleBook.Services;

public class RepositoryConfiguration
{
    public const string PathConfiguration = "Postgres";
    public required string Host {  get; set; }
    public required string Port { get; set; }
    public required string Database {  get; set; }
    public required string User { get; set; }
    public required string Password { get; set; }
}
