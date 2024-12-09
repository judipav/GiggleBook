using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Chirper.Services;

public class DialogRepository : IDialogRepository
{
    private readonly string _connectionString;
    public DialogRepository(IOptions<RepositoryConfiguration> settings)
    {
        var configuration = settings.Value;
        _connectionString = $"Host={configuration.Write.Host};Port={configuration.Write.Port};Username={configuration.Write.User};Password={configuration.Write.Password};Database={configuration.Write.Database}";
        
    }
    
    public async IAsyncEnumerable<Chirp> List(Guid from, Guid to, int offset, [EnumeratorCancellation] CancellationToken token)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT * FROM public.list_message(:from, :to, :skip, :take)";
        command.Parameters.Add(new NpgsqlParameter("from", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = from;
        command.Parameters.Add(new NpgsqlParameter("to", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = to;
        command.Parameters.Add(new NpgsqlParameter("skip", NpgsqlTypes.NpgsqlDbType.Integer)).Value = offset;
        command.Parameters.Add(new NpgsqlParameter("take", NpgsqlTypes.NpgsqlDbType.Integer)).Value = 10;

        var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            yield return new Chirp(from, to, reader.GetString(2),reader.GetDateTime(3));
        
    }

    public async Task Send(Chirp chirp, CancellationToken token)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT * FROM public.send_message(:from, :to, :message)";
        command.Parameters.Add(new NpgsqlParameter("from", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = chirp.From;
        command.Parameters.Add(new NpgsqlParameter("to", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = chirp.To;
        command.Parameters.Add(new NpgsqlParameter("message", NpgsqlTypes.NpgsqlDbType.Text)).Value = chirp.Message;

        var reader = await command.ExecuteNonQueryAsync(token);
    }

    private NpgsqlConnection GetConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        
        return connection;
    }
}


