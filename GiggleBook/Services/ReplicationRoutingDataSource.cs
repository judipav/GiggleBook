using Npgsql;
using Microsoft.Extensions.Options;
using GiggleBook.Interfaces;

namespace GiggleBook.Services;
public class ReplicationRoutingDataSource : IReplicationRoutingDataSource
{
    private readonly RepositoryConfiguration _configuration;
    private readonly List<string> _readReplicas;
    private int _slaveIndex = -1;
    private readonly string _masterConnection;

    public ReplicationRoutingDataSource(IOptions<RepositoryConfiguration> options)
    {
        _configuration = options.Value;
        
        _masterConnection = $"Host={_configuration.Write.Host};Port={_configuration.Write.Port};Username={_configuration.Write.User};Password={_configuration.Write.Password};Database={_configuration.Write.Database}";
        _readReplicas = _configuration.Cqrs ? _configuration.Readonly.Select(s => $"Host={s.Host};Port={s.Port};Username={s.User};Password={s.Password};Database={s.Database}").ToList()  
            : new List<string> { _masterConnection };
    }

    public NpgsqlConnection GetConnection(string commandText) 
    => IsReadCommand(commandText) 
        ? GetReadConnection()  
        : CreateConnection(_masterConnection);
    

    private bool IsReadCommand(string commandText)
    {
        commandText = commandText.Trim().ToUpper();
        var readCommands = new List<string>
        {
            "SELECT", "WITH", "EXPLAIN", "SHOW", "DESCRIBE", "PRAGMA", "FETCH"
        };

        return readCommands.Any(cmd => commandText.StartsWith(cmd));
    }

    private NpgsqlConnection GetReadConnection()
    {
        if (_readReplicas.Count == 0)
        {
            throw new InvalidOperationException("Нет доступных реплик для чтения.");
        }

        _slaveIndex = (_slaveIndex + 1) % _readReplicas.Count;
        return CreateConnection(_readReplicas[_slaveIndex]);
    }

    private NpgsqlConnection CreateConnection(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString); 
        connection.Open();
        return connection;
    }
}