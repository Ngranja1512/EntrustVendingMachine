using VendingMachine.Domain.ValueObjects;

namespace VendingMachine.Domain.Entities;

/// <summary>A product available for purchase in the vending machine.</summary>
public sealed class Product
{
    /// <summary>Unique identifier for this product.</summary>
    public Guid Id { get; }

    /// <summary>Display name of the product.</summary>
    public string Name { get; }

    /// <summary>Price of the product in pence.</summary>
    public Money Price { get; }

    /// <summary>Number of units currently in stock.</summary>
    public int Quantity { get; private set; }

    /// <param name="id">Unique product identifier.</param>
    /// <param name="name">Display name. Must not be null or whitespace.</param>
    /// <param name="price">Price in pence. Must not be null.</param>
    /// <param name="quantity">Initial stock quantity. Must be zero or greater.</param>
    public Product(Guid id, string name, Money price, int quantity)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(price);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        Id = id;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    /// <summary>Whether any units are available for purchase.</summary>
    public bool IsInStock => Quantity > 0;

    /// <summary>Decrements the stock by one unit.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the product is already out of stock.</exception>
    internal void Dispense()
    {
        if (!IsInStock)
        {
            throw new InvalidOperationException($"Product '{Name}' is out of stock.");
        }

        Quantity--;
    }

    /// <summary>Adds units to the stock.</summary>
    /// <param name="count">Number of units to add. Must be greater than zero.</param>
    internal void Restock(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Restock count must be greater than zero.");
        }

        Quantity += count;
    }
}
