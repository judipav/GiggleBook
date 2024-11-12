using Npgsql;

namespace GiggleBook.Interfaces;

public interface IReplicationRoutingDataSource {
    NpgsqlConnection GetConnection(string commandText);
}
