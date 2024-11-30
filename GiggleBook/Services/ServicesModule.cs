using Autofac;
using Autofac.Core.Lifetime;
using GiggleBook.Auth;
using GiggleBook.Services.Instrumentation;
using GiggleBook.Services.ServiceException;

namespace GiggleBook.Services;

public class ServicesModule : Module
{
    private readonly string _redisConnectionString;
    public ServicesModule()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"{nameof(ServicesModule)}.json", optional: false, reloadOnChange: true)
            .Build();

        _redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString")?? throw new CommonServiceException(1001, "Ошибка конфигурации модуля ServiceModule");
    }

    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ReplicationRoutingDataSource>().AsSelf().AsImplementedInterfaces();

        var redisCache = new RedisService(_redisConnectionString);
        
        builder.RegisterInstance(redisCache).AsSelf().SingleInstance();

        builder.RegisterType<PostService>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();      
        
        builder.RegisterType<UsersRepository>().AsSelf().AsImplementedInterfaces();

        builder.RegisterType<AuthenticationManager>().AsSelf().AsImplementedInterfaces();

        builder.RegisterType<RepositoryServiceInstrumentation>().AsSelf().SingleInstance();
    }
}
