using Npgsql;


namespace Authorization.Services;

public interface IReplicationRoutingDataSource
{
    NpgsqlConnection GetConnection(ConnectionType connectionType);
}