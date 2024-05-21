using GiggleBook.Dto;
using GiggleBook.Services;
using GiggleBook.Services.ServiceException;
using Microsoft.Extensions.Options;

using Npgsql;

namespace GiggleBook.Initialize;

public class FillDbHostedService : IHostedService
{
    private readonly RepositoryConfiguration _configuration;
    private readonly string _connectionString;
    private readonly ILogger<FillDbHostedService> _logger;

    public FillDbHostedService(IOptions<RepositoryConfiguration> options, ILogger<FillDbHostedService> logger)
    {
        _configuration = options.Value;

        _logger = logger;

        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        if (!string.IsNullOrEmpty(dbHost))
        {
            _configuration.Host = dbHost;
        }
        _connectionString = $"Host={_configuration.Host};Username={_configuration.User};Password={_configuration.Password};Database={_configuration.Database}";
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //return FillDb();
        return Task.CompletedTask;
    }

    private async Task FillDb()
    {
        try
        {
            char[] gender = ['M', 'F'];

            var randomGender = new Random();

            int counter = 0;
            await foreach (string rec in File.ReadLinesAsync("DemoData/people.v2.csv"))
            {
                string[] fields = rec.Split(',');
                try
                {
                    var user = new User
                    {
                        UserName = $"bulkuser{counter++}",
                        FirstName = fields[0].Split(' ')[0],
                        SecondName = fields[0].Split(' ')[1],
                        BirthDate = DateTime.Parse(fields[1]),
                        City = fields[2],
                        Biography = $"bio 4 bulkuser",
                        Sex = gender[randomGender.Next(0, gender.Length)]
                    };

                    await RegisterUserAsync(user, user.UserName);
                }
                catch (Exception e1)
                {
                    _logger.LogInformation(e1.Message);
                }                
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<bool> RegisterUserAsync(User user, string password)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = "select * from register_user(:f_name, :s_name, :dt_birth, :bio, :city, :sword, :u_name, :u_sex)";
        command.Parameters.Add(new NpgsqlParameter("f_name", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = user.FirstName;
        command.Parameters.Add(new NpgsqlParameter("s_name", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = user.SecondName;
        command.Parameters.Add(new NpgsqlParameter("dt_birth", NpgsqlTypes.NpgsqlDbType.Date)).Value = user.BirthDate;
        command.Parameters.Add(new NpgsqlParameter("bio", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = user.Biography;
        command.Parameters.Add(new NpgsqlParameter("city", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = user.City;
        command.Parameters.Add(new NpgsqlParameter("sword", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = EncryptionService.EncryptPassword(password);
        command.Parameters.Add(new NpgsqlParameter("u_name", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = user.UserName;
        command.Parameters.Add(new NpgsqlParameter("u_sex", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = user.Sex;

        try
        {
            var response = await command.ExecuteNonQueryAsync();
            
            if (response > 0)
                return true;

            return false;
        }
        catch (NpgsqlException ex)
        {
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        finally 
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
