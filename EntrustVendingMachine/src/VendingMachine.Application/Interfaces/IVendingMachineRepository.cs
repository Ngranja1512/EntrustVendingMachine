using VendingMachine.Domain.Entities;

namespace VendingMachine.Application.Interfaces;

/// <summary>Provides access to the persisted vending machine state.</summary>
public interface IVendingMachineRepository
{
    /// <summary>Retrieves the current vending machine aggregate.</summary>
    Task<Domain.Entities.VendingMachine> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the current vending machine state.</summary>
    Task SaveAsync(Domain.Entities.VendingMachine machine, CancellationToken cancellationToken = default);
}
