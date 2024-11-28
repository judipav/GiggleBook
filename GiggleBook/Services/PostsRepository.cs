using GiggleBook.Dto;
using GiggleBook.Interfaces;
using GiggleBook.Services.ServiceException;
using Npgsql;
using System.Data;


namespace GiggleBook.Services;

public class PostsRepository : IPostsRepository
{
    private readonly IReplicationRoutingDataSource _routingDataSource;
    public PostsRepository(IReplicationRoutingDataSource routingDataSource)
    {
        _routingDataSource = routingDataSource;
    }

    public async Task<Post> AddAsync(Post post)
    {
        using var connection = _routingDataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_create(:userId, :post)";
        command.Parameters.Add(new NpgsqlParameter("userId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = post.AuthorId;
        command.Parameters.Add(new NpgsqlParameter("post", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = post.Text;
        
        try
        {
            var response = await command.ExecuteReaderAsync();

            if (!response.Read())
                throw new CommonServiceException(152, "Не удалось запостить");

            return new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4) };
        }
        catch (NpgsqlException ex)
        {
            throw new CommonServiceException(ex.ErrorCode, ex.Message);
        }
        catch (Exception ex) 
        {
            throw new CommonServiceException(501, ex.Message);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = _routingDataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_delete(:postId)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = id;

        try
        {
            var response = await command.ExecuteReaderAsync();
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }

    public async Task<Post> GetAsync(Guid id)
    {
        using var connection = _routingDataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_get(:postId)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = id;

        try
        {
            var response = await command.ExecuteReaderAsync();
            
            if (!response.Read())
                throw new CommonServiceException(641, "Не удалось найти пост с таким ID");

            return new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4) };
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }

    public async Task<IEnumerable<Post>> GetUserPosts(Guid userId)
    {
        using var connection = _routingDataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_get_all(:postId)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = userId;

        try
        {
            var response = await command.ExecuteReaderAsync();
            List<Post> posts = new();              

            while (response.Read())
            {
                posts.Add(new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4)});
            }
            
            return posts;
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }

    public async Task<Post> UpdateAsync(Guid postId, string text)
    {
        using var connection = _routingDataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_update(:postId, :postText)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = postId;
        command.Parameters.Add(new NpgsqlParameter("postText", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = text;
        
        try
        {
            var response = await command.ExecuteReaderAsync();
            
            if (!response.Read())
                throw new CommonServiceException(641, "Не удалось найти пост с таким ID");

            return new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4) };
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }

    public async Task<IEnumerable<Post>> GetFeedAsync(Guid userId)
    {
        using var connection = _routingDataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_feed(:user_id)";
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = userId;

        try
        {
            var response = await command.ExecuteReaderAsync();
            List<Post> posts = new();
            if (!response.Read())
                throw new CommonServiceException(641, "Лента пользователя пуста");

            while (response.Read())
                posts.Add(new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4)});
                        
            return posts;
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }
}