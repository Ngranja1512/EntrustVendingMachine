namespace VendingMachine.Domain.ValueObjects;

/// <summary>Represents a monetary amount stored in pence to avoid floating-point precision issues.</summary>
public sealed class Money : IEquatable<Money>
{
    /// <summary>Amount in pence. Must be non-negative.</summary>
    public int Pence { get; }

    /// <param name="pence">Amount in pence. Must be zero or greater.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pence"/> is negative.</exception>
    public Money(int pence)
    {
        if (pence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pence), "Amount cannot be negative.");
        }

        Pence = pence;
    }

    /// <summary>Zero money.</summary>
    public static Money Zero => new(0);

    /// <summary>Creates a <see cref="Money"/> from a pence value.</summary>
    public static Money FromPence(int pence) => new(pence);

    /// <summary>Returns the amount formatted as a GBP string (e.g. "£1.50" or "25p").</summary>
    public override string ToString()
    {
        if (Pence >= 100)
        {
            return $"£{Pence / 100m:0.00}";
        }

        return $"{Pence}p";
    }

    public static Money operator +(Money left, Money right) => new(left.Pence + right.Pence);
    public static Money operator -(Money left, Money right) => new(left.Pence - right.Pence);
    public static bool operator >(Money left, Money right) => left.Pence > right.Pence;
    public static bool operator <(Money left, Money right) => left.Pence < right.Pence;
    public static bool operator >=(Money left, Money right) => left.Pence >= right.Pence;
    public static bool operator <=(Money left, Money right) => left.Pence <= right.Pence;

    public bool Equals(Money? other) => other is not null && Pence == other.Pence;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => Pence.GetHashCode();
    public static bool operator ==(Money left, Money right) => left.Equals(right);
    public static bool operator !=(Money left, Money right) => !left.Equals(right);
}
