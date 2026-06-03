using System.ComponentModel.DataAnnotations;
using VendingMachine.Domain.Enums;

namespace VendingMachine.Api.Models;

/// <summary>Request body for purchasing a product.</summary>
public sealed class PurchaseRequest
{
    /// <summary>The unique identifier of the product to purchase.</summary>
    [Required]
    public Guid ProductId { get; init; }

    /// <summary>Total amount of money inserted, in pence. Must be greater than zero.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount inserted must be greater than zero.")]
    public int AmountInsertedPence { get; init; }
}

/// <summary>A product to add or restock in the machine.</summary>
public sealed class ProductRequest
{
    /// <summary>Unique product identifier. If loading a new product, provide a new <see cref="Guid"/>.</summary>
    [Required]
    public Guid Id { get; init; }

    /// <summary>Display name of the product.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Price of the product in pence. Must be greater than zero.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public int PricePence { get; init; }

    /// <summary>Number of units to load. Must be greater than zero.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; init; }
}

/// <summary>Request body for loading products.</summary>
public sealed class LoadProductsRequest
{
    /// <summary>Products to load or restock.</summary>
    [Required]
    [MinLength(1)]
    public List<ProductRequest> Products { get; init; } = new();
}

/// <summary>Request body for loading change into the coin float.</summary>
public sealed class LoadChangeRequest
{
    /// <summary>Map of coin denomination to the number of coins to add.</summary>
    [Required]
    public Dictionary<CoinDenomination, int> Coins { get; init; } = new();
}
