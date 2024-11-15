using Npgsql;
using GiggleBook.Interfaces;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace GiggleBook.Services;

public enum ConnectionType{
    Write,
    Readonly
}
public class ReplicationRoutingDataSource : IReplicationRoutingDataSource
{
    private readonly string[] _slaves;
    private readonly string _master;
    private readonly RepositoryConfiguration _configuration;

    public ReplicationRoutingDataSource(IOptions<RepositoryConfiguration> configuration)
    {
        _configuration = configuration.Value;
        _master = $"Host={_configuration.Write.Host};Port={_configuration.Write.Port};Username={_configuration.Write.User};Password={_configuration.Write.Password};Database={_configuration.Write.Database}";
        _slaves = _configuration.Cqrs ? _configuration.Readonly.Select(s => $"Host={s.Host};Port={s.Port};Username={s.User};Password={s.Password};Database={s.Database}").ToArray()  
            : [_master];
    }

    public NpgsqlConnection GetConnection(ConnectionType connectionType) 
    {
        if (_configuration.Cqrs && connectionType == ConnectionType.Readonly)
        {
            return CreateConnection(_slaves[Random.Shared.Next(0, _slaves.Length)]);;

            // var st = new StackTrace();
            // for (int i = 1; i < st.FrameCount; i++)
            // {
            //     var frame = st.GetFrame(i);
            //     var method = frame?.GetMethod();
            //     if (method != null && method.GetCustomAttributes(typeof(ReplicaReadOnlyAttribute), false).Any())
            //     {
                    
            //     }
            // }
        }
        
        return CreateConnection(_master);
    }

    private NpgsqlConnection CreateConnection(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString); 
        connection.Open();
        return connection;
    }
}