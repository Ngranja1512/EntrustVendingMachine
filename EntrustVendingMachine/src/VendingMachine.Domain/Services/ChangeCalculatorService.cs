using VendingMachine.Domain.Common;
using VendingMachine.Domain.Enums;
using VendingMachine.Domain.ValueObjects;

namespace VendingMachine.Domain.Services;

/// <summary>
/// Calculates the optimal change to return using a greedy algorithm (largest denomination first).
/// </summary>
public sealed class ChangeCalculatorService
{
    private static readonly IReadOnlyList<CoinDenomination> DenominationsDescending =
        Enum.GetValues<CoinDenomination>()
            .OrderByDescending(d => (int)d)
            .ToList();

    /// <summary>
    /// Calculates the coins to return for the given change amount using denominations available in the machine.
    /// </summary>
    /// <param name="changeAmountPence">The amount to return in pence.</param>
    /// <param name="availableCoins">Current coin inventory: denomination → count.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the <see cref="Change"/> to dispense,
    /// or a failure result when the exact amount cannot be made from available coins.
    /// </returns>
    public Result<Change> Calculate(int changeAmountPence, IReadOnlyDictionary<CoinDenomination, int> availableCoins)
    {
        ArgumentNullException.ThrowIfNull(availableCoins);

        if (changeAmountPence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(changeAmountPence), "Change amount cannot be negative.");
        }

        if (changeAmountPence == 0)
        {
            return Result.Success(Change.Empty);
        }

        var workingInventory = availableCoins.ToDictionary(kv => kv.Key, kv => kv.Value);
        var selectedCoins = new Dictionary<CoinDenomination, int>();
        var remaining = changeAmountPence;

        foreach (var denomination in DenominationsDescending)
        {
            if (remaining == 0)
            {
                break;
            }

            var denominationValue = (int)denomination;
            if (denominationValue > remaining)
            {
                continue;
            }

            if (!workingInventory.TryGetValue(denomination, out var available) || available == 0)
            {
                continue;
            }

            var coinsNeeded = remaining / denominationValue;
            var coinsToUse = Math.Min(coinsNeeded, available);

            if (coinsToUse > 0)
            {
                selectedCoins[denomination] = coinsToUse;
                remaining -= coinsToUse * denominationValue;
            }
        }

        if (remaining != 0)
        {
            return Result.Failure<Change>(
                $"Unable to provide exact change of {new Money(changeAmountPence)}. " +
                "Please use a different payment amount.");
        }

        return Result.Success(new Change(selectedCoins));
    }
}
