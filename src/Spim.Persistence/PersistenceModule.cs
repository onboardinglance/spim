using Autofac;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Spim.Application.Common.Interfaces;
using Spim.Persistence.Interceptors;

namespace Spim.Persistence;

public sealed class PersistenceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.Register(c => c.Resolve<ApplicationDbContext>())
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.RegisterType<AuditableEntityInterceptor>()
            .As<ISaveChangesInterceptor>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ApplicationDbContextInitialiser>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
