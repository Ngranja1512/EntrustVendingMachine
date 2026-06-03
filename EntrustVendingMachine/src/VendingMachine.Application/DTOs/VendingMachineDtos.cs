using VendingMachine.Domain.Enums;

namespace VendingMachine.Application.DTOs;

/// <summary>Data about a single product returned to callers.</summary>
/// <param name="Id">Unique product identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="PricePence">Price in pence.</param>
/// <param name="Quantity">Units in stock.</param>
public sealed record ProductDto(Guid Id, string Name, int PricePence, int Quantity);

/// <summary>Result of a successful purchase transaction.</summary>
/// <param name="Product">The product that was dispensed.</param>
/// <param name="Change">Coins returned as change, keyed by denomination value in pence.</param>
public sealed record PurchaseResultDto(ProductDto Product, IReadOnlyDictionary<CoinDenomination, int> Change);

/// <summary>Current state of the vending machine.</summary>
/// <param name="Products">All products in the machine (in and out of stock).</param>
/// <param name="CoinFloat">Current coin float: denomination → count.</param>
public sealed record MachineStateDto(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyDictionary<CoinDenomination, int> CoinFloat);
