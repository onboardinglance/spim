using Autofac;
using Spim.Application.Common.Interfaces;
using Spim.Infrastructure.Email;

namespace Spim.Infrastructure;

public sealed class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(TimeProvider.System).As<TimeProvider>().SingleInstance();

        builder.RegisterType<SmtpEmailService>().As<IEmailService>().InstancePerLifetimeScope();
    }
}
