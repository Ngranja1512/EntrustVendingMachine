namespace VendingMachine.Application.Commands;

/// <summary>Represents a product to be loaded into the vending machine.</summary>
/// <param name="Id">Unique product identifier. Use <see cref="Guid.NewGuid"/> when adding a new product.</param>
/// <param name="Name">Display name of the product.</param>
/// <param name="PricePence">Price of the product in pence.</param>
/// <param name="Quantity">Number of units to load.</param>
public sealed record ProductToLoad(Guid Id, string Name, int PricePence, int Quantity);

/// <summary>Command to load or restock products in the vending machine.</summary>
/// <param name="Products">Products to load. Must not be empty.</param>
public sealed record LoadProductsCommand(IReadOnlyList<ProductToLoad> Products);
