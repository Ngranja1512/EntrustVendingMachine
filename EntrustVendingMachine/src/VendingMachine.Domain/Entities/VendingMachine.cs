using VendingMachine.Domain.Common;
using VendingMachine.Domain.Enums;
using VendingMachine.Domain.Services;
using VendingMachine.Domain.ValueObjects;

namespace VendingMachine.Domain.Entities;

/// <summary>
/// Aggregate root representing the vending machine. Manages product inventory and coin float,
/// and orchestrates purchase transactions.
/// </summary>
public sealed class VendingMachine
{
    private readonly Dictionary<Guid, Product> _products = new();
    private readonly Dictionary<CoinDenomination, int> _coinFloat = new();
    private readonly ChangeCalculatorService _changeCalculator;

    /// <summary>Creates a new vending machine with an empty inventory and coin float.</summary>
    public VendingMachine(ChangeCalculatorService changeCalculator)
    {
        ArgumentNullException.ThrowIfNull(changeCalculator);
        _changeCalculator = changeCalculator;
    }

    /// <summary>Read-only view of products currently in the machine.</summary>
    public IReadOnlyDictionary<Guid, Product> Products => _products;

    /// <summary>Read-only view of coins available in the machine's float.</summary>
    public IReadOnlyDictionary<CoinDenomination, int> CoinFloat => _coinFloat;

    /// <summary>
    /// Loads or restocks products into the machine. Existing products are restocked by the supplied quantity;
    /// new products are added.
    /// </summary>
    /// <param name="products">Products to load. Must not be null.</param>
    /// <returns>A successful result, or a failure result if any product definition is invalid.</returns>
    public Result LoadProducts(IEnumerable<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        var productList = products.ToList();

        if (productList.Count == 0)
        {
            return Result.Failure("Product list cannot be empty.");
        }

        foreach (var product in productList)
        {
            if (_products.TryGetValue(product.Id, out var existing))
            {
                existing.Restock(product.Quantity);
            }
            else
            {
                _products[product.Id] = product;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Loads coins into the machine's float.
    /// </summary>
    /// <param name="coins">Denomination-to-count map of coins to load. Must not be null.</param>
    /// <returns>A successful result, or a failure result if any count is invalid.</returns>
    public Result LoadChange(IReadOnlyDictionary<CoinDenomination, int> coins)
    {
        ArgumentNullException.ThrowIfNull(coins);

        if (coins.Count == 0)
        {
            return Result.Failure("Coin map cannot be empty.");
        }

        foreach (var (denomination, count) in coins)
        {
            if (count <= 0)
            {
                return Result.Failure($"Count for {denomination} must be greater than zero.");
            }

            _coinFloat[denomination] = _coinFloat.GetValueOrDefault(denomination) + count;
        }

        return Result.Success();
    }

    /// <summary>
    /// Attempts to purchase a product by product id with the supplied payment.
    /// On success the product is dispensed and change is returned; the machine state is updated atomically.
    /// </summary>
    /// <param name="productId">The id of the product to purchase.</param>
    /// <param name="amountInsertedPence">The amount of money inserted in pence.</param>
    /// <returns>
    /// A successful result containing the dispensed product and change,
    /// or a failure result describing why the purchase could not be completed.
    /// </returns>
    public Result<(Product Product, Change Change)> Purchase(Guid productId, int amountInsertedPence)
    {
        if (amountInsertedPence <= 0)
        {
            return Result.Failure<(Product, Change)>("Amount inserted must be greater than zero.");
        }

        if (!_products.TryGetValue(productId, out var product))
        {
            return Result.Failure<(Product, Change)>($"Product with id '{productId}' was not found.");
        }

        if (!product.IsInStock)
        {
            return Result.Failure<(Product, Change)>($"'{product.Name}' is out of stock.");
        }

        var price = product.Price.Pence;

        if (amountInsertedPence < price)
        {
            var shortfall = new Money(price - amountInsertedPence);
            return Result.Failure<(Product, Change)>(
                $"Insufficient funds. {shortfall} more is required.");
        }

        var changeAmountPence = amountInsertedPence - price;

        // The API accepts a raw pence amount, not specific coins. Per this design, the inserted
        // amount is treated as an abstract payment value rather than a set of physical coins
        // being added to the float. The machine's coin float is managed explicitly via the
        // LoadChange method. Therefore, we only deduct the dispensed change from the float.
        var changeResult = _changeCalculator.Calculate(changeAmountPence, _coinFloat);

        if (changeResult.IsFailure)
        {
            return Result.Failure<(Product, Change)>(changeResult.Error!);
        }

        // Commit state: deduct change coins from float, dispense product.
        var change = changeResult.Value!;
        foreach (var (denomination, count) in change.Coins)
        {
            _coinFloat[denomination] -= count;
            if (_coinFloat[denomination] == 0)
            {
                _coinFloat.Remove(denomination);
            }
        }

        product.Dispense();

        return Result.Success((product, change));
    }
}
