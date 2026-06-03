using Microsoft.Extensions.Logging;
using VendingMachine.Application.Commands;
using VendingMachine.Application.DTOs;
using VendingMachine.Application.Interfaces;
using VendingMachine.Domain.Common;
using VendingMachine.Domain.Entities;
using VendingMachine.Domain.ValueObjects;

namespace VendingMachine.Application.Services;

/// <summary>
/// Application service that orchestrates vending machine use cases.
/// All public methods are asynchronous and return <see cref="Result{T}"/> for foreseeable failures.
/// </summary>
public sealed class VendingMachineService : IVendingMachineService
{
    private readonly IVendingMachineRepository _repository;
    private readonly ILogger<VendingMachineService> _logger;

    public VendingMachineService(IVendingMachineRepository repository, ILogger<VendingMachineService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Purchases a product using the specified payment amount.</summary>
    public async Task<Result<PurchaseResultDto>> PurchaseAsync(
        PurchaseProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var machine = await _repository.GetAsync(cancellationToken);
        var result = machine.Purchase(command.ProductId, command.AmountInsertedPence);

        if (result.IsFailure)
        {
            _logger.LogInformation("Purchase failed for product {ProductId}: {Error}", command.ProductId, result.Error);
            return Result.Failure<PurchaseResultDto>(result.Error);
        }

        await _repository.SaveAsync(machine, cancellationToken);

        var (product, change) = result.Value;
        return Result.Success(new PurchaseResultDto(
            MapProduct(product),
            change.Coins));
    }

    /// <summary>Loads or restocks products in the machine.</summary>
    public async Task<Result> LoadProductsAsync(
        LoadProductsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Products == null || command.Products.Count == 0)
        {
            return Result.Failure("Products list cannot be empty.");
        }

        var domainProducts = new List<Product>();
        foreach (var p in command.Products)
        {
            if (string.IsNullOrWhiteSpace(p.Name))
            {
                return Result.Failure("Product name cannot be empty.");
            }

            if (p.PricePence <= 0)
            {
                return Result.Failure($"Price for '{p.Name}' must be greater than zero.");
            }

            if (p.Quantity <= 0)
            {
                return Result.Failure($"Quantity for '{p.Name}' must be greater than zero.");
            }

            domainProducts.Add(new Product(p.Id, p.Name, new Money(p.PricePence), p.Quantity));
        }

        var machine = await _repository.GetAsync(cancellationToken);
        var result = machine.LoadProducts(domainProducts);

        if (result.IsFailure)
        {
            return result;
        }

        await _repository.SaveAsync(machine, cancellationToken);
        return Result.Success();
    }

    /// <summary>Loads coins into the machine's change float.</summary>
    public async Task<Result> LoadChangeAsync(
        LoadChangeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Coins == null || command.Coins.Count == 0)
        {
            return Result.Failure("Coins map cannot be empty.");
        }

        var machine = await _repository.GetAsync(cancellationToken);
        var result = machine.LoadChange(command.Coins);

        if (result.IsFailure)
        {
            return result;
        }

        await _repository.SaveAsync(machine, cancellationToken);
        return Result.Success();
    }

    /// <summary>Returns the full current state of the machine.</summary>
    public async Task<MachineStateDto> GetMachineStateAsync(CancellationToken cancellationToken = default)
    {
        var machine = await _repository.GetAsync(cancellationToken);

        return new MachineStateDto(
            machine.Products.Values.Select(MapProduct).ToList(),
            machine.CoinFloat);
    }

    /// <summary>Returns only products currently in stock.</summary>
    public async Task<IReadOnlyList<ProductDto>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
    {
        var machine = await _repository.GetAsync(cancellationToken);

        return machine.Products.Values
            .Where(p => p.IsInStock)
            .Select(MapProduct)
            .ToList();
    }

    private static ProductDto MapProduct(Product product) =>
        new(product.Id, product.Name, product.Price.Pence, product.Quantity);
}
