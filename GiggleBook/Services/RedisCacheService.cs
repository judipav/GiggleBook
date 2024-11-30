using StackExchange.Redis;

namespace GiggleBook.Services;

public class RedisService 
{ 
    private readonly Lazy<ConnectionMultiplexer> _connection; 
    private readonly string _connectionString;
    public RedisService(string redisConnectionString) 
    { 
        _connection = new Lazy<ConnectionMultiplexer>(() =>  
            ConnectionMultiplexer.Connect(redisConnectionString)); 
        _connectionString = redisConnectionString;
    } 

    public IDatabase Database 
    { 
        get 
        {
            try
            {
                return _connection.Value.GetDatabase(); 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось получить БД Redis", ex); 
            }
        } 
    } 

    public IServer Server {
        get 
        {
            try
            {
                return _connection.Value.GetServer(_connectionString);
            }
            catch (System.Exception)
            {
                
                throw;
            }
        }
    }
}