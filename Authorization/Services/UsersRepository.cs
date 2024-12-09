
using Authorization.Services.Entities;
using Authorization.Services.ServiceException;
using Npgsql;
using System.Data;

namespace Authorization.Services;

public interface IUsersRepository
{
    Task<User> AuthUserAsync(string name, string token);
}

public class UsersRepository : IUsersRepository
{
    private readonly IReplicationRoutingDataSource _routingDataSource;

    public UsersRepository(IReplicationRoutingDataSource dataSource)
    {
        _routingDataSource = dataSource;
    }

    public async Task<User> AuthUserAsync(string name, string token)
    {
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
                return user; 
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
}
