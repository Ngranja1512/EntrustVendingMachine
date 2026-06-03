using VendingMachine.Domain.Enums;

namespace VendingMachine.Application.Commands;

/// <summary>Command to load coins into the vending machine's change float.</summary>
/// <param name="Coins">Map of coin denomination to quantity to load. All quantities must be greater than zero.</param>
public sealed record LoadChangeCommand(IReadOnlyDictionary<CoinDenomination, int> Coins);
