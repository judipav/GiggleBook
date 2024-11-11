using GiggleBook.Dto;
using GiggleBook.Interfaces;
using GiggleBook.Services.ServiceException;
using GiggleBook.Utilities;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace GiggleBook.Services;

public class RepositoryService : IRepository
{
    private readonly RepositoryConfiguration _configuration;
    private readonly ILogger<RepositoryService> _logger;
    private readonly string _writeConnectionString;
    private readonly string _readonlyConnectionString;

    public RepositoryService(IOptions<RepositoryConfiguration> options, ILogger<RepositoryService> logger)
    {
        _configuration = options.Value;
        _logger = logger;

        var dbWrite = Environment.GetEnvironmentVariable("DB_RW_HOST");
        if (!string.IsNullOrEmpty(dbWrite))
        {
            _logger.LogInformation($"Обнаружена переменная окружения [DB_RW_HOST] {dbWrite}");

            _configuration.Write.Host = dbWrite;
        }

        var dbReadOnly = Environment.GetEnvironmentVariable("DB_RO_HOST");
        if (!string.IsNullOrEmpty(dbReadOnly))
        {
            _logger.LogInformation($"Обнаружена переменная окружения [DB_RO_HOST] {dbReadOnly}");
            _configuration.Write.Host = dbWrite;
        }

        _writeConnectionString = $"Host={_configuration.Write.Host};Port={_configuration.Write.Port};Username={_configuration.Write.User};Password={_configuration.Write.Password};Database={_configuration.Write.Database}";
        _readonlyConnectionString = _configuration.Cqrs ? $"Host={_configuration.Readonly.Host};Port={_configuration.Readonly.Port};Username={_configuration.Readonly.User};Password={_configuration.Readonly.Password};Database={_configuration.Readonly.Database}" 
            : _writeConnectionString;
    }

    public async Task<User> AuthUserAsync(string name, string token)
    {
        using var connection = new NpgsqlConnection(_writeConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("token", token);
        command.CommandText = $"select * from auth_user(:name, :token)";

        try
        {
            var response = await command.ExecuteReaderAsync();
            
            if (response != null && response.Read())
            {
                var sex = response.GetString(7);

                return new User
                {
                    Id = response.GetGuid(0),
                    UserName = response.GetString(1),
                    FirstName = response.GetString(2),
                    SecondName = response.GetString(3),
                    BirthDate = response.GetDateTime(4),
                    Biography = response.GetString(5),
                    City = response.GetString(6),
                    Sex = response.GetChar(7)
                };
            }
            else
            {
                throw new CommonServiceException(404, "Пользователь не найден");
            }
        }
        catch (NpgsqlException ex)
        {
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
    }

    public UserDto[] FindUser(string firstName, string secondName)
    {
        using NpgsqlConnection connection = new NpgsqlConnection(_readonlyConnectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.Parameters.AddWithValue("fname", firstName);
        command.Parameters.AddWithValue("sname", secondName);
        command.CommandText = $"select * from find_user(:fname, :sname)";

        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);

        DataTable table = new DataTable();

        try
        {
            adapter.Fill(table);

            if (table.Rows.Count > 0)
            {
                List<UserDto> users = new List<UserDto>();

                foreach (DataRow row in table.Rows)
                {
                    users.Add(row.ToObject<UserDto>());
                }

                return users.ToArray();
            }
            else
            {
                throw new CommonServiceException(404, "Пользователь не найден");
            }
        }
        catch (NpgsqlException ex)
        {
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
    }

    public async Task<User> GetUserAsync(string id)
    {
        using var connection = new NpgsqlConnection(_readonlyConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.Parameters.AddWithValue("id", Guid.Parse(id));
        command.CommandText = $"select * from get_user(:id)";
                
        var response = await command.ExecuteReaderAsync();

        try
        {
            if (response != null && response.Read())
            {
                return new User
                {
                    Id = response.GetGuid(0),
                    UserName = response.GetString(1),
                    FirstName = response.GetString(2),
                    SecondName = response.GetString(3),
                    BirthDate = response.GetDateTime(4),
                    Biography = response.GetString(5),
                    City = response.GetString(6),
                    Sex = response.GetChar(7)
                };
            }
            else
            {
                throw new CommonServiceException(404, "Пользователь не найден");
            }
        }        
        catch (NpgsqlException ex)
        {

            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        catch (Exception e1)
        {
            throw new CommonServiceException(143, e1.Message);
        }

    }

    public async Task<User> RegisterUserAsync(User user, string password)
    {
        using var connection = new NpgsqlConnection(_writeConnectionString);
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
            var response = await command.ExecuteReaderAsync();

            if (response.Read() && Guid.TryParse(response[0].ToString(), out Guid userId) )
                return await GetUserAsync(userId.ToString());

            throw new CommonServiceException(152, "Не удалось зарегистрировать пользователя");
        }
        catch (NpgsqlException ex)
        {
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
    }
}
