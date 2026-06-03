namespace VendingMachine.Application.Commands;

/// <summary>Command to purchase a product from the vending machine.</summary>
/// <param name="ProductId">The unique identifier of the product to purchase.</param>
/// <param name="AmountInsertedPence">The total amount of money inserted, in pence.</param>
public sealed record PurchaseProductCommand(Guid ProductId, int AmountInsertedPence);
