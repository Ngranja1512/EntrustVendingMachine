using VendingMachine.Domain.Common;
using VendingMachine.Domain.Entities;
using VendingMachine.Domain.Enums;
using VendingMachine.Domain.Services;
using VendingMachine.Domain.ValueObjects;

namespace VendingMachine.Domain.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldHaveIsSuccessTrue_WhenCreated()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void Failure_ShouldHaveIsSuccessFalse_WhenCreatedWithError()
    {
        var result = Result.Failure("something went wrong");
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("something went wrong", result.Error);
    }

    [Fact]
    public void SuccessGeneric_ShouldCarryValue_WhenCreated()
    {
        var result = Result.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void FailureGeneric_ShouldHaveNullValue_WhenCreatedWithError()
    {
        var result = Result.Failure<int>("bad");
        Assert.False(result.IsSuccess);
        Assert.Equal("bad", result.Error);
    }
}

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_ShouldCreateMoney_WhenPenceIsZero()
    {
        var money = new Money(0);
        Assert.Equal(0, money.Pence);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPenceIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1));
    }

    [Fact]
    public void ToString_ShouldReturnPoundsFormat_WhenAmountIsOneHundredOrMore()
    {
        var money = new Money(150);
        Assert.Equal("£1.50", money.ToString());
    }

    [Fact]
    public void ToString_ShouldReturnPenceFormat_WhenAmountIsLessThanOneHundred()
    {
        var money = new Money(25);
        Assert.Equal("25p", money.ToString());
    }

    [Fact]
    public void Addition_ShouldReturnSum_WhenAddingTwoMoneyValues()
    {
        var a = new Money(50);
        var b = new Money(75);
        Assert.Equal(new Money(125), a + b);
    }

    [Fact]
    public void Equality_ShouldBeTrue_WhenBothHaveSamePence()
    {
        Assert.Equal(new Money(100), new Money(100));
    }
}

public sealed class ChangeTests
{
    [Fact]
    public void Constructor_ShouldFilterZeroCounts_WhenInitialised()
    {
        var change = new Change(new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.OnePound] = 1,
            [CoinDenomination.TwentyPence] = 0
        });

        Assert.Single(change.Coins);
        Assert.Equal(1, change.Coins[CoinDenomination.OnePound]);
    }

    [Fact]
    public void TotalPence_ShouldReturnCorrectSum_WhenMultipleCoins()
    {
        var change = new Change(new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.OnePound] = 1,
            [CoinDenomination.FiftyPence] = 2
        });

        Assert.Equal(200, change.TotalPence);
    }

    [Fact]
    public void IsEmpty_ShouldBeTrue_WhenNoCoins()
    {
        Assert.True(Change.Empty.IsEmpty);
    }
}

public sealed class ChangeCalculatorServiceTests
{
    private readonly ChangeCalculatorService _sut = new();

    [Fact]
    public void Calculate_ShouldReturnEmptyChange_WhenAmountIsZero()
    {
        var result = _sut.Calculate(0, new Dictionary<CoinDenomination, int>());
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsEmpty);
    }

    [Fact]
    public void Calculate_ShouldReturnCorrectChange_WhenExactCoinsAvailable()
    {
        var coins = new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.FiftyPence] = 5,
            [CoinDenomination.TwentyPence] = 5,
            [CoinDenomination.TenPence] = 5
        };

        var result = _sut.Calculate(80, coins);

        Assert.True(result.IsSuccess);
        Assert.Equal(80, result.Value!.TotalPence);
    }

    [Fact]
    public void Calculate_ShouldUseFewerLargerCoins_WhenGreedyIsOptimal()
    {
        var coins = new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.FiftyPence] = 2,
            [CoinDenomination.TwentyPence] = 5,
            [CoinDenomination.TenPence] = 5
        };

        var result = _sut.Calculate(50, coins);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Coins[CoinDenomination.FiftyPence]);
        Assert.False(result.Value.Coins.ContainsKey(CoinDenomination.TwentyPence));
    }

    [Fact]
    public void Calculate_ShouldReturnFailure_WhenExactChangeCannotBeMade()
    {
        var coins = new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.TwentyPence] = 2
        };

        var result = _sut.Calculate(30, coins);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenAmountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(-1, new Dictionary<CoinDenomination, int>()));
    }
}

public sealed class VendingMachineAggregateTests
{
    private static Entities.VendingMachine CreateMachine() =>
        new(new ChangeCalculatorService());

    private static Product CreateProduct(int pricePence = 100, int quantity = 5) =>
        new(Guid.NewGuid(), "Test Product", new Money(pricePence), quantity);

    private static Dictionary<CoinDenomination, int> DefaultCoins() => new()
    {
        [CoinDenomination.OnePound] = 10,
        [CoinDenomination.FiftyPence] = 10,
        [CoinDenomination.TwentyPence] = 10,
        [CoinDenomination.TenPence] = 10,
        [CoinDenomination.FivePence] = 10,
        [CoinDenomination.TwoPence] = 10,
        [CoinDenomination.OnePence] = 10
    };

    [Fact]
    public void LoadProducts_ShouldAddProduct_WhenProductIsNew()
    {
        var machine = CreateMachine();
        var product = CreateProduct();

        var result = machine.LoadProducts(new[] { product });

        Assert.True(result.IsSuccess);
        Assert.Contains(product.Id, machine.Products.Keys);
    }

    [Fact]
    public void LoadProducts_ShouldIncreaseQuantity_WhenProductAlreadyExists()
    {
        var machine = CreateMachine();
        var product = CreateProduct(quantity: 3);
        machine.LoadProducts(new[] { product });

        var restock = new Product(product.Id, product.Name, product.Price, 5);
        machine.LoadProducts(new[] { restock });

        Assert.Equal(8, machine.Products[product.Id].Quantity);
    }

    [Fact]
    public void LoadProducts_ShouldReturnFailure_WhenListIsEmpty()
    {
        var machine = CreateMachine();
        var result = machine.LoadProducts(Array.Empty<Product>());
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void LoadChange_ShouldAddCoins_WhenValidCoinsProvided()
    {
        var machine = CreateMachine();
        var result = machine.LoadChange(DefaultCoins());

        Assert.True(result.IsSuccess);
        Assert.Equal(10, machine.CoinFloat[CoinDenomination.OnePound]);
    }

    [Fact]
    public void Purchase_ShouldReturnProductAndNoChange_WhenExactAmountInserted()
    {
        var machine = CreateMachine();
        var product = CreateProduct(pricePence: 100);
        machine.LoadProducts(new[] { product });
        machine.LoadChange(DefaultCoins());
        machine.InsertCredit(CoinDenomination.OnePound);

        var result = machine.Purchase(product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(product.Id, result.Value.Product.Id);
        Assert.True(result.Value.Change.IsEmpty);
        Assert.Equal(0, machine.UserCreditPence);
    }

    [Fact]
    public void Purchase_ShouldReturnChange_WhenOverpaymentInserted()
    {
        var machine = CreateMachine();
        var product = CreateProduct(pricePence: 100);
        machine.LoadProducts(new[] { product });
        machine.LoadChange(DefaultCoins());
        machine.InsertCredit(CoinDenomination.OnePound);
        machine.InsertCredit(CoinDenomination.FiftyPence);

        var result = machine.Purchase(product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.Change.TotalPence);
    }

    [Fact]
    public void Purchase_ShouldReturnFailure_WhenInsufficientFundsInserted()
    {
        var machine = CreateMachine();
        var product = CreateProduct(pricePence: 100);
        machine.LoadProducts(new[] { product });
        machine.InsertCredit(CoinDenomination.FiftyPence);

        var result = machine.Purchase(product.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("Insufficient", result.Error);
        Assert.Equal(50, machine.UserCreditPence);
    }

    [Fact]
    public void Purchase_ShouldReturnFailure_WhenProductNotFound()
    {
        var machine = CreateMachine();
        machine.InsertCredit(CoinDenomination.TwoPounds);

        var result = machine.Purchase(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public void Purchase_ShouldReturnFailure_WhenProductIsOutOfStock()
    {
        var machine = CreateMachine();
        var product = CreateProduct(quantity: 1);
        machine.LoadProducts(new[] { product });
        machine.LoadChange(DefaultCoins());
        machine.InsertCredit(CoinDenomination.OnePound);
        machine.Purchase(product.Id);

        machine.InsertCredit(CoinDenomination.OnePound);

        var result = machine.Purchase(product.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("out of stock", result.Error);
    }

    [Fact]
    public void Purchase_ShouldDeductProductQuantity_WhenSuccessful()
    {
        var machine = CreateMachine();
        var product = CreateProduct(pricePence: 100, quantity: 3);
        machine.LoadProducts(new[] { product });
        machine.LoadChange(DefaultCoins());
        machine.InsertCredit(CoinDenomination.OnePound);

        machine.Purchase(product.Id);

        Assert.Equal(2, machine.Products[product.Id].Quantity);
    }

    [Fact]
    public void Purchase_ShouldDeductChangeFromFloat_WhenChangeIsReturned()
    {
        var machine = CreateMachine();
        var product = CreateProduct(pricePence: 100);
        machine.LoadProducts(new[] { product });
        machine.LoadChange(new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.FiftyPence] = 2
        });
        machine.InsertCredit(CoinDenomination.OnePound);
        machine.InsertCredit(CoinDenomination.FiftyPence);

        machine.Purchase(product.Id);

        Assert.Equal(2, machine.CoinFloat[CoinDenomination.FiftyPence]);
    }

    [Fact]
    public void Purchase_ShouldReturnFailure_WhenMachineCannotMakeChange()
    {
        var machine = CreateMachine();
        var product = CreateProduct(pricePence: 100);
        machine.LoadProducts(new[] { product });
        machine.LoadChange(new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.TwoPounds] = 5
        });
        machine.InsertCredit(CoinDenomination.TwoPounds);

        var result = machine.Purchase(product.Id);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InsertCredit_ShouldAddToMachineCredit_WhenCoinIsInserted()
    {
        var machine = CreateMachine();

        var result = machine.InsertCredit(CoinDenomination.OnePound);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, machine.UserCreditPence);
    }

    [Fact]
    public void ReturnCredit_ShouldReturnInsertedAmountAsCoins_WhenCreditExists()
    {
        var machine = CreateMachine();
        machine.LoadChange(DefaultCoins());
        machine.InsertCredit(CoinDenomination.OnePound);

        var result = machine.ReturnCredit();

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.TotalPence);
        Assert.Equal(0, machine.UserCreditPence);
    }

    [Fact]
    public void ReturnCredit_ShouldReturnFailure_WhenNoCreditExists()
    {
        var machine = CreateMachine();

        var result = machine.ReturnCredit();

        Assert.True(result.IsFailure);
    }
}