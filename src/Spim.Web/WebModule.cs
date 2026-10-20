using Autofac;
using Spim.Application.Common.Interfaces;
using Spim.Web.Services;

namespace Spim.Web;

public sealed class WebModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CurrentUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
    }
}
