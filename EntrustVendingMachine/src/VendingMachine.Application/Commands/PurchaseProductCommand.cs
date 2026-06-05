namespace VendingMachine.Application.Commands;

/// <summary>Command to purchase a product from the vending machine.</summary>
/// <param name="ProductId">The unique identifier of the product to purchase.</param>
public sealed record PurchaseProductCommand(Guid ProductId);
