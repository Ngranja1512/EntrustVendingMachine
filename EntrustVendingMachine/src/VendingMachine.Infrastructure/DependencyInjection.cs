using Microsoft.Extensions.DependencyInjection;
using VendingMachine.Application.Interfaces;
using VendingMachine.Infrastructure.Repositories;

namespace VendingMachine.Infrastructure;

/// <summary>Extension methods for registering Infrastructure layer services.</summary>
public static class DependencyInjection
{
    /// <summary>Registers Infrastructure layer services with the DI container.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IVendingMachineRepository, VendingMachineRepository>();
        return services;
    }
}
