using System;
using Autofac;

namespace Authorization.Services;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<UsersRepository>().AsImplementedInterfaces();
        builder.RegisterType<AuthUserService>().AsImplementedInterfaces();
        builder.RegisterType<ReplicationRoutingDataSource>().AsImplementedInterfaces();
        
    }
}
