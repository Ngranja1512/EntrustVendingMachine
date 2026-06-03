using Microsoft.Extensions.DependencyInjection;
using VendingMachine.Application.Interfaces;
using VendingMachine.Application.Services;

namespace VendingMachine.Application;

/// <summary>Extension methods for registering Application layer services.</summary>
public static class DependencyInjection
{
    /// <summary>Registers Application layer services with the DI container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<IVendingMachineService, VendingMachineService>();
        return services;
    }
}
