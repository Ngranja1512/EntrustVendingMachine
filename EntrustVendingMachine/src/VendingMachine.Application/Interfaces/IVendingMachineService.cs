using VendingMachine.Application.Commands;
using VendingMachine.Application.DTOs;
using VendingMachine.Domain.Common;
using VendingMachine.Domain.Enums;

namespace VendingMachine.Application.Interfaces;

/// <summary>Orchestrates vending machine use cases.</summary>
public interface IVendingMachineService
{
    Task<Result<PurchaseResultDto>> PurchaseAsync(PurchaseProductCommand command, CancellationToken cancellationToken = default);
    Task<Result> InsertCreditAsync(InsertCreditCommand command, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyDictionary<CoinDenomination, int>>> ReturnCreditAsync(CancellationToken cancellationToken = default);
    Task<Result> LoadProductsAsync(LoadProductsCommand command, CancellationToken cancellationToken = default);
    Task<Result> LoadChangeAsync(LoadChangeCommand command, CancellationToken cancellationToken = default);
    Task<MachineStateDto> GetMachineStateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);
}
