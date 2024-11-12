using Npgsql;
using Microsoft.Extensions.Options;
using GiggleBook.Interfaces;
using System.Diagnostics;

namespace GiggleBook.Services;
public class ReplicationRoutingDataSource : IReplicationRoutingDataSource
{
    private readonly RepositoryConfiguration _configuration;
    private readonly List<string> _readReplicas;
    private readonly string _masterConnection;
    private readonly IGlobalVariables _globalVariables;

    public ReplicationRoutingDataSource(IOptions<RepositoryConfiguration> options, IGlobalVariables globalVariables)
    {
        _configuration = options.Value;
        _globalVariables = globalVariables;
        
        _masterConnection = $"Host={_configuration.Write.Host};Port={_configuration.Write.Port};Username={_configuration.Write.User};Password={_configuration.Write.Password};Database={_configuration.Write.Database}";
        _readReplicas = _configuration.Cqrs ? _configuration.Readonly.Select(s => $"Host={s.Host};Port={s.Port};Username={s.User};Password={s.Password};Database={s.Database}").ToList()  
            : new List<string> { _masterConnection };
    }

    public NpgsqlConnection GetConnection() 
    {
        var st = new StackTrace();
        for (int i = 1; i < st.FrameCount; i++)
        {
            var frame = st.GetFrame(i);
            var method = frame?.GetMethod();
            if (method != null && method.GetCustomAttributes(typeof(ReplicaReadOnlyAttribute), false).Any())
            {
                return GetReadConnection();;
            }
        }
        return CreateConnection(_masterConnection);
    }

    private NpgsqlConnection GetReadConnection()
    {
        if (_readReplicas.Count == 0)
            throw new InvalidOperationException("Нет доступных реплик для чтения.");

        _globalVariables.CurrentSlave = (_globalVariables.CurrentSlave + 1) % _readReplicas.Count;
        return CreateConnection(_readReplicas[_globalVariables.CurrentSlave]);
    }

    private NpgsqlConnection CreateConnection(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString); 
        connection.Open();
        return connection;
    }
}