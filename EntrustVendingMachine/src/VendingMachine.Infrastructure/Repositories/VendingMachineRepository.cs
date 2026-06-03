using VendingMachine.Application.Interfaces;
using VendingMachine.Domain.Services;

namespace VendingMachine.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IVendingMachineRepository"/>.
/// Maintains a single vending machine instance for the lifetime of the application.
/// </summary>
public sealed class VendingMachineRepository : IVendingMachineRepository
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Domain.Entities.VendingMachine _machine;

    public VendingMachineRepository(ChangeCalculatorService changeCalculatorService)
    {
        ArgumentNullException.ThrowIfNull(changeCalculatorService);
        _machine = new Domain.Entities.VendingMachine(changeCalculatorService);
    }

    /// <inheritdoc/>
    public async Task<Domain.Entities.VendingMachine> GetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _machine;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Domain.Entities.VendingMachine machine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machine);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _machine = machine;
        }
        finally
        {
            _lock.Release();
        }
    }
}
