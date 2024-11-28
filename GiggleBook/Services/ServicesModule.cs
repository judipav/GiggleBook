using Autofac;
using GiggleBook.Auth;
using GiggleBook.Services.Instrumentation;

namespace GiggleBook.Services;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<UsersRepository>().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<PostsRepository>().AsSelf().AsImplementedInterfaces();

        builder.RegisterType<AuthenticationManager>().AsSelf().AsImplementedInterfaces();

        builder.RegisterType<ReplicationRoutingDataSource>().AsSelf().AsImplementedInterfaces();

        builder.RegisterType<RepositoryServiceInstrumentation>().AsSelf().SingleInstance();
    }
}
