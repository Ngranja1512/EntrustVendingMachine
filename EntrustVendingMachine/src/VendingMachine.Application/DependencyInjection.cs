using Microsoft.Extensions.DependencyInjection;
using VendingMachine.Application.Services;

namespace VendingMachine.Application;

/// <summary>Extension methods for registering Application layer services.</summary>
public static class DependencyInjection
{
    /// <summary>Registers Application layer services with the DI container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<VendingMachineService>();
        return services;
    }
}
