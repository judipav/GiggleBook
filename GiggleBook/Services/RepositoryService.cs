using GiggleBook.Dto;
using GiggleBook.Interfaces;
using GiggleBook.Services.Instrumentation;
using GiggleBook.Services.ServiceException;
using GiggleBook.Utilities;
using Npgsql;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;


namespace GiggleBook.Services;

public class RepositoryService : IRepository
{
    private readonly IReplicationRoutingDataSource _routingDataSource;
    private readonly RepositoryServiceInstrumentation _instrumentation;
    public RepositoryService(IReplicationRoutingDataSource dataSource, RepositoryServiceInstrumentation instrumentation)
    {
        _instrumentation = instrumentation;
        _routingDataSource = dataSource;
    }

    public async Task<User> AuthUserAsync(string name, string token)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity("AuthUserAsync");

        using var connection = _routingDataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("token", token);
        command.CommandText = $"select * from auth_user(:name, :token)";
        
        try
        {
            var response = await command.ExecuteReaderAsync();
            
            if (response != null && response.Read())
            {
                var user = new User
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
                activity?.SetStatus(ActivityStatusCode.Ok);
                return user; 
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw new CommonServiceException(404, "Пользователь не найден");
            }
        }
        catch (NpgsqlException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        finally 
        {
            activity?.SetTag("executionTime", DateTime.UtcNow);
        }
    }

    public UserDto[] FindUser(string firstName, string secondName)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity("AuthUserAsync");

        using var connection = _routingDataSource.GetConnection(ConnectionType.Readonly);
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
                    users.Add(row.ToObject<UserDto>());

                activity?.SetStatus(ActivityStatusCode.Ok);

                return users.ToArray();
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw new CommonServiceException(404, "Пользователь не найден");
            }
        }
        catch (NpgsqlException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        finally
        {
            activity?.SetTag("executionTime", DateTime.UtcNow);
        }
    }

    public async Task<User> GetUserAsync(string id)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity("GetUserAsync");

        using var connection = _routingDataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.Parameters.AddWithValue("id", Guid.Parse(id));
        command.CommandText = $"select * from get_user(:id)";
                
        var response = await command.ExecuteReaderAsync();
        try
        {
            if (response != null && response.Read())
            {
                var user = new User
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
                activity?.SetStatus(ActivityStatusCode.Ok);
                return user; 
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw new CommonServiceException(404, "Пользователь не найден");
            }
        }        
        catch (NpgsqlException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        catch (Exception e1)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw new CommonServiceException(143, e1.Message);
        }
        finally 
        {
            activity?.SetTag("executionTime", DateTime.UtcNow);
        }
    }

    public async Task<User> RegisterUserAsync(User user, string password)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity("RegisterUserAsync");
        
        using var connection = _routingDataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
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
            {
                var addedUser = await GetUserAsync(userId.ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);
                RepositoryServiceInstrumentation.SuccessfulWritesCounter.Add(1);
                return addedUser;
            }

            activity?.SetStatus(ActivityStatusCode.Error);
            RepositoryServiceInstrumentation.FailedWritesCounter.Add(1);
            throw new CommonServiceException(152, "Не удалось зарегистрировать пользователя");
        }
        catch (NpgsqlException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            RepositoryServiceInstrumentation.FailedWritesCounter.Add(1);
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error);
            RepositoryServiceInstrumentation.FailedWritesCounter.Add(1);
            throw new CommonServiceException(501, ex.Message);
        }
        finally 
        {
            activity?.SetTag("executionTime", DateTime.UtcNow);
        }
    }
}
