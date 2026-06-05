using Microsoft.Extensions.Diagnostics.HealthChecks;
using VendingMachine.Application.Interfaces;

namespace VendingMachine.Api.HealthChecks;

public sealed class VendingMachineHealthCheck : IHealthCheck
{
    private readonly IVendingMachineRepository _repository;

    public VendingMachineHealthCheck(IVendingMachineRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var machine = await _repository.GetAsync(cancellationToken);
            var productCount = machine.Products.Count;
            var coinCount = machine.CoinFloat.Sum(kv => kv.Value);

            return HealthCheckResult.Healthy(
                $"State accessible. Products: {productCount}, coins in float: {coinCount}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot access vending machine state.", ex);
        }
    }
}
