using VendingMachine.Domain.Enums;

namespace VendingMachine.Application.Commands;

/// <summary>Command to insert a single coin into the vending machine.</summary>
/// <param name="Denomination">The denomination of the coin to insert.</param>
public sealed record InsertCreditCommand(CoinDenomination Denomination);
