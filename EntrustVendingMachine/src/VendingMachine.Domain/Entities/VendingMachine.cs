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
    private int _userCreditPence;

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

    /// <summary>Current user credit inserted into the machine, in pence.</summary>
    public int UserCreditPence => _userCreditPence;

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

    /// <summary>Accepts a single coin inserted by the user, adding its value to the credit balance and the coin to the float.</summary>
    /// <param name="denomination">The denomination of the coin inserted.</param>
    /// <returns>A successful result.</returns>
    public Result InsertCredit(CoinDenomination denomination)
    {
        var pence = (int)denomination;
        _userCreditPence += pence;
        _coinFloat[denomination] = _coinFloat.GetValueOrDefault(denomination) + 1;
        return Result.Success();
    }

    /// <summary>Returns the currently inserted credit as change from the machine float.</summary>
    /// <returns>Returned credit as coins, or a failure if no credit is present or exact change cannot be made.</returns>
    public Result<Change> ReturnCredit()
    {
        if (_userCreditPence <= 0)
        {
            return Result.Failure<Change>("No credit to return.");
        }

        var changeResult = _changeCalculator.Calculate(_userCreditPence, _coinFloat);

        if (changeResult.IsFailure)
        {
            return Result.Failure<Change>(changeResult.Error!);
        }

        var change = changeResult.Value!;
        DeductChangeFromFloat(change);
        _userCreditPence = 0;

        return Result.Success(change);
    }

    /// <summary>
    /// Attempts to purchase a product by product id using currently inserted machine credit.
    /// On success the product is dispensed and change is returned; the machine state is updated atomically.
    /// </summary>
    /// <param name="productId">The id of the product to purchase.</param>
    /// <returns>
    /// A successful result containing the dispensed product and change,
    /// or a failure result describing why the purchase could not be completed.
    /// </returns>
    public Result<(Product Product, Change Change)> Purchase(Guid productId)
    {
        if (!_products.TryGetValue(productId, out var product))
        {
            return Result.Failure<(Product, Change)>($"Product with id '{productId}' was not found.");
        }

        if (!product.IsInStock)
        {
            return Result.Failure<(Product, Change)>($"'{product.Name}' is out of stock.");
        }

        var price = product.Price.Pence;

        if (_userCreditPence < price)
        {
            var shortfall = new Money(price - _userCreditPence);
            return Result.Failure<(Product, Change)>(
                $"Insufficient funds. {shortfall} more is required.");
        }

        var changeAmountPence = _userCreditPence - price;

        var changeResult = _changeCalculator.Calculate(changeAmountPence, _coinFloat);

        if (changeResult.IsFailure)
        {
            return Result.Failure<(Product, Change)>(changeResult.Error!);
        }

        var change = changeResult.Value!;
        DeductChangeFromFloat(change);

        product.Dispense();
        _userCreditPence = 0;

        return Result.Success((product, change));
    }

    private void DeductChangeFromFloat(Change change)
    {
        foreach (var (denomination, count) in change.Coins)
        {
            _coinFloat[denomination] -= count;
            if (_coinFloat[denomination] == 0)
            {
                _coinFloat.Remove(denomination);
            }
        }
    }
}
