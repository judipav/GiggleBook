using System;
using Autofac;

namespace Chirper.Services;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<DialogRepository>().AsImplementedInterfaces();
    }

}
