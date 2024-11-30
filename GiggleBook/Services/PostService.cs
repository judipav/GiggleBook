using GiggleBook.Dto;
using GiggleBook.Interfaces;
using GiggleBook.Services.ServiceException;
using Npgsql;
using StackExchange.Redis;
using System.Data;
using System.Runtime.CompilerServices;


namespace GiggleBook.Services;

public class PostService : IPostsRepository
{
    private readonly IDatabase _redis;
    private readonly IServer _redisServer;
    private const int MAX_POSTS = 1000;
    private readonly IReplicationRoutingDataSource _dataSource;
    public PostService(IReplicationRoutingDataSource dataSource, RedisService redisService)
    {
        _dataSource = dataSource;
        _redis = redisService.Database;
        _redisServer = redisService.Server;
    }

    public async Task AddAsync(Post post, CancellationToken token)
    {
        await AddCacheAsync(post, token);
        await AddDbAsync(post, token);
    }

    private async Task AddCacheAsync(Post post, CancellationToken token)
    {
        await _redis.HashSetAsync($"post:{post.Id}",
        [
            new HashEntry("author_id", post.AuthorId.ToString()),
            new HashEntry("updated_at", post.UpdatedAt.ToString()),
            new HashEntry("text", post.Text)
        ]);
        
        await _redis.StringSetAsync($"{post.Id}:rating", 0);

        var friends = (await GetFriends(post.AuthorId, token)).ToList();
        friends.Add(post.AuthorId);
        foreach (var friend in friends)
        {
            await _redis.ListLeftPushAsync($"timeline:{friend}", post.Id.ToString()); //формируем фид постов друзей при добавлении нового поста
            await _redis.ListTrimAsync($"timeline:{friend}", 0, MAX_POSTS - 1); // сносим последний пост
        }
    }
    private async Task<IEnumerable<Post>> GetDbFeedAsync(Guid userId, CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_feed(:user_id)";
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = userId;

        try
        {
            var response = await command.ExecuteReaderAsync(token);
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

    private async IAsyncEnumerable<Post> GetDbFeedExceptAsync(Guid userId, Guid[] except, int limit, [EnumeratorCancellation] CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_feed_except(:user_id, :posts, :lim)";
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = userId;
        command.Parameters.Add(new NpgsqlParameter("posts", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Uuid)).Value = except;
        command.Parameters.Add(new NpgsqlParameter("lim", NpgsqlTypes.NpgsqlDbType.Integer)).Value = limit;

        var response = await command.ExecuteReaderAsync(token);
        List<Post> posts = new();
        if (!response.Read())
            throw new CommonServiceException(641, "Лента пользователя пуста");

        while (response.Read()) 
        {
            yield return new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4)};
        }
    }

    public async IAsyncEnumerable<Post> GetFeedAsync(Guid userId, [EnumeratorCancellation] CancellationToken token)
    {
        int count = 1000;
        var postIds = await _redis.ListRangeAsync($"timeline:{userId}", 0, count - 1);
        var posts = new List<Post>();

        foreach (var postId in postIds)
        {
            await _redis.StringIncrementAsync($"{postId}:rating");

            var postData = await _redis.HashGetAllAsync($"post:{postId}");
            if (postData.Length > 0)
            {
                var post = new Post
                {
                    Id = Guid.Parse(postId),
                    AuthorId = Guid.Parse(postData.FirstOrDefault(x => x.Name == "author_id").Value),
                    Text = postData.FirstOrDefault(x => x.Name == "text").Value,
                    UpdatedAt = DateTime.Parse(postData.FirstOrDefault(x => x.Name == "updated_at").Value)
                };
                posts.Add(post);
                yield return post; 
            }
        }
        //раскомментировать если нужно подгружать недостающие посты из БД
        // if (posts.Count < count) 
        //     yield return (Post)GetDbFeedExceptAsync(userId, posts.Select(p => p.Id).ToArray(), count - posts.Count, token);
        
    }

    private async Task<Post> AddDbAsync(Post post, CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_create(:userId, :post)";
        command.Parameters.Add(new NpgsqlParameter("userId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = post.AuthorId;
        command.Parameters.Add(new NpgsqlParameter("post", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = post.Text;
        
        try
        {
            var response = await command.ExecuteReaderAsync(token);

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

    public async Task DeleteAsync(Guid id, CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_delete(:postId)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = id;

        try
        {
            var response = await command.ExecuteReaderAsync(token);
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }

    public async Task<Post> GetAsync(Guid id, CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_get(:postId)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = id;

        try
        {
            var response = await command.ExecuteReaderAsync(token);
            
            if (!response.Read())
                throw new CommonServiceException(641, "Не удалось найти пост с таким ID");

            return new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4) };
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }

    public async Task<IEnumerable<Post>> GetUserPosts(Guid userId, CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_get_all(:postId)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = userId;

        try
        {
            var response = await command.ExecuteReaderAsync(token);
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

    public async Task<Post> UpdateAsync(Guid postId, string text, CancellationToken token)
    {
        using var connection = _dataSource.GetConnection(ConnectionType.Write);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "select * from post_update(:postId, :postText)";
        command.Parameters.Add(new NpgsqlParameter("postId", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = postId;
        command.Parameters.Add(new NpgsqlParameter("postText", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = text;
        
        try
        {
            var response = await command.ExecuteReaderAsync(token);
            
            if (!response.Read())
                throw new CommonServiceException(641, "Не удалось найти пост с таким ID");

            return new Post{ Id = response.GetGuid(0), Text = response.GetString(1), AuthorId = response.GetGuid(2), CreatedAt = response.GetDateTime(3), UpdatedAt = response.GetDateTime(4) };
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(654, ex.Message);
        }
    }
    
    public async Task CacheMostActiveUsersAsync(CancellationToken token)
    {
        List<Guid> mostActiveUsers = (await MostActiveUsersAsync(token)).ToList();
            Console.WriteLine("Начинаю обработку постов наиболее активных пользователей...\n");
        
        // чтобы не затягивать прогрев кэша:
        mostActiveUsers = mostActiveUsers[..5];

        int userIndex = 1;
        int totalUsers = mostActiveUsers.Count;

        foreach (var item in mostActiveUsers)
        {
            var posts = (await GetUserPosts(item, token)).ToList();

            Console.ForegroundColor = ConsoleColor.Green; 
            Console.WriteLine($"[{userIndex}/{totalUsers}] Обрабатываю посты пользователя: {item} ({posts.Count()} постов найдено)");
            Console.ResetColor();

            foreach(var post in posts) {
                await AddCacheAsync(post, token);
            } 

            var friends = await GetFriends(item, token); //тянем френдов и их посты тоже кэшируем
            int friendIndex = 1;
            foreach (var friend in friends)
            {
                var friendPosts = (await GetUserPosts(friend, token)).ToList();
                Console.WriteLine($"[{friendIndex}/{friends.Count()}] Обрабатываю посты пользователя: {friend} ({friendPosts.Count()} постов найдено)");
                foreach(var friendPost in friendPosts) 
                {
                    await AddCacheAsync(friendPost, token);
                } 
                friendIndex++;
            }
            
            userIndex++;
        }
    }

    private async Task<IEnumerable<Guid>> MostActiveUsersAsync(CancellationToken token)
    {
        List<Guid> mostActiveUsers = new ();
        using var connection = _dataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT * FROM most_active()";

        try
        {
            var reader = await command.ExecuteReaderAsync(token);
            while (reader.Read())
            {
                mostActiveUsers.Add(reader.GetGuid(0));
            }
            return mostActiveUsers;
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(768, $"Ошибка при прогреве кэша {ex.Message}");
        }
    }

    private async Task<IEnumerable<Guid>> GetFriends(Guid userId, CancellationToken token)
    {
        List<Guid> friends = new ();
        using var connection = _dataSource.GetConnection(ConnectionType.Readonly);
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT * FROM get_friends(:user_id)";
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Uuid)).Value = userId;

        try
        {
            var reader = await command.ExecuteReaderAsync(token);
            while (reader.Read())
            {
                friends.Add(reader.GetGuid(0));
            }
            return friends;
        }
        catch (Exception ex)
        {
            throw new CommonServiceException(768, $"Ошибка при прогреве кэша {ex.Message}");
        }
    }

    private async Task DecreasePostRatingAsync(string postId) => await _redis.StringDecrementAsync($"{postId}:rating");
    
    private async Task CheckAndRemoveOlfPostsFixedCount(int count) 
    {
        int removedPostsCount = 0;
        long cursor = 0;
        do
        {
            var keys = new List<RedisKey>();
            var result = _redisServer.Keys(cursor: cursor, pattern: "post:*", pageSize: 100).ToArray();
            keys.AddRange(result);
            
            cursor = (int)(cursor + result.Length);
            
            foreach (var key in keys)
            {
                var guid = key.ToString().Substring("post:".Length);
                var ratingKey = $"{guid}:rating";

                var rating = (int)await _redis.StringGetAsync(ratingKey);

                if (rating <= 0)
                {
                    await _redis.KeyDeleteAsync(key);
                    await _redis.KeyDeleteAsync(ratingKey);
                    Console.WriteLine($"Пост удален из кэша по расписанию: {key}");

                    removedPostsCount++; 

                    if (removedPostsCount >= count)
                    {
                        break; 
                    }
                }
            }
        }
        while (cursor != 0 || removedPostsCount < count);
    }

    private async Task CheckAndRemoveOldPosts()
    {
        var keys = new List<RedisKey>();
        var cursor = 0;

        do
        {
            var result = _redisServer.Keys(cursor: cursor, pattern: "post:*", pageSize: 100).ToArray();
            keys.AddRange(result);
            cursor = (int)(cursor + result.Length);
        } while (cursor != 0);

        foreach (var key in keys)
        {
            var guid = key.ToString().Substring("post:".Length);
            var ratingKey = $"{guid}:rating";

            var rating = (int)await _redis.StringGetAsync(ratingKey);
            
            if (rating <= 0)
            {
                await _redis.KeyDeleteAsync(key);
                await _redis.KeyDeleteAsync(ratingKey);
                Console.WriteLine($"Пост удален из кэша по расписанию.");
            }
        }
    }
}