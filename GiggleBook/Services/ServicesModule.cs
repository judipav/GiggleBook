using Autofac;
using GiggleBook.Auth;

namespace GiggleBook.Services;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<RepositoryService>().AsSelf().AsImplementedInterfaces();

        builder.RegisterType<AuthenticationManager>().AsSelf().AsImplementedInterfaces();
    }
}
