using VendingMachine.Domain.Enums;

namespace VendingMachine.Domain.ValueObjects;

/// <summary>Represents a collection of coins to be returned as change.</summary>
public sealed class Change : IEquatable<Change>
{
    private readonly Dictionary<CoinDenomination, int> _coins;

    /// <summary>Initialises an empty change collection.</summary>
    public Change() => _coins = new Dictionary<CoinDenomination, int>();

    /// <summary>Initialises a change collection from an existing denomination map.</summary>
    /// <param name="coins">Map of denomination to coin count. All counts must be non-negative.</param>
    public Change(IReadOnlyDictionary<CoinDenomination, int> coins)
    {
        ArgumentNullException.ThrowIfNull(coins);

        foreach (var (denomination, count) in coins)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coins), $"Count for {denomination} cannot be negative.");
            }
        }

        _coins = new Dictionary<CoinDenomination, int>(coins.Where(kv => kv.Value > 0));
    }

    /// <summary>Read-only view of the denomination-to-count map.</summary>
    public IReadOnlyDictionary<CoinDenomination, int> Coins => _coins;

    /// <summary>Total value of all coins in pence.</summary>
    public int TotalPence => _coins.Sum(kv => (int)kv.Key * kv.Value);

    /// <summary>Whether the change collection contains no coins.</summary>
    public bool IsEmpty => _coins.Count == 0;

    /// <summary>An empty change collection.</summary>
    public static Change Empty => new();

    public bool Equals(Change? other)
    {
        if (other is null)
        {
            return false;
        }

        if (_coins.Count != other._coins.Count)
        {
            return false;
        }

        foreach (var (denomination, count) in _coins)
        {
            if (!other._coins.TryGetValue(denomination, out var otherCount) || count != otherCount)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is Change other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (denomination, count) in _coins.OrderBy(kv => kv.Key))
        {
            hash.Add(denomination);
            hash.Add(count);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(Change left, Change right) => left.Equals(right);
    public static bool operator !=(Change left, Change right) => !left.Equals(right);
}
