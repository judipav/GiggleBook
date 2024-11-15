using GiggleBook.Services;
using Npgsql;

namespace GiggleBook.Interfaces;

public interface IReplicationRoutingDataSource 
{
    NpgsqlConnection GetConnection(ConnectionType connectionType);
}