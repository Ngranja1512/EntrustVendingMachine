using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VendingMachine.Application.Commands;
using VendingMachine.Application.Interfaces;
using VendingMachine.Application.Services;
using VendingMachine.Domain.Enums;
using VendingMachine.Domain.Entities;
using VendingMachine.Domain.Services;
using VendingMachine.Domain.ValueObjects;

namespace VendingMachine.Application.Tests;

public sealed class VendingMachineServiceTests
{
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Domain.Entities.VendingMachine CreateSeededMachine()
    {
        var machine = new Domain.Entities.VendingMachine(new ChangeCalculatorService());
        machine.LoadProducts(new[]
        {
            new Product(ProductId, "Cola", new Money(100), 5)
        });
        machine.LoadChange(new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.FiftyPence]  = 10,
            [CoinDenomination.TwentyPence] = 10,
            [CoinDenomination.TenPence]    = 10
        });
        return machine;
    }

    private static (VendingMachineService Service, IVendingMachineRepository Repository) CreateSut(
        Domain.Entities.VendingMachine? machine = null)
    {
        var repo = Substitute.For<IVendingMachineRepository>();
        repo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(machine ?? CreateSeededMachine());
        var service = new VendingMachineService(repo, NullLogger<VendingMachineService>.Instance);
        return (service, repo);
    }

    [Fact]
    public async Task PurchaseAsync_ShouldReturnSuccess_WhenProductExistsAndFundsAreSufficient()
    {
        var (service, repo) = CreateSut();

        var result = await service.PurchaseAsync(new PurchaseProductCommand(ProductId, 100));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cola", result.Value!.Product.Name);
        await repo.Received(1).SaveAsync(Arg.Any<Domain.Entities.VendingMachine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseAsync_ShouldReturnChange_WhenOverpaymentProvided()
    {
        var (service, _) = CreateSut();

        var result = await service.PurchaseAsync(new PurchaseProductCommand(ProductId, 150));

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value!.Change.Values.Sum(count =>
            result.Value.Change.Keys.Zip(result.Value.Change.Values)
                .Sum(kv => (int)kv.First * kv.Second)));
    }

    [Fact]
    public async Task PurchaseAsync_ShouldReturnFailure_WhenInsufficientFunds()
    {
        var (service, repo) = CreateSut();

        var result = await service.PurchaseAsync(new PurchaseProductCommand(ProductId, 50));

        Assert.True(result.IsFailure);
        Assert.Contains("Insufficient", result.Error);
        await repo.DidNotReceive().SaveAsync(Arg.Any<Domain.Entities.VendingMachine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseAsync_ShouldReturnFailure_WhenProductNotFound()
    {
        var (service, _) = CreateSut();

        var result = await service.PurchaseAsync(new PurchaseProductCommand(Guid.NewGuid(), 200));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task LoadProductsAsync_ShouldReturnSuccess_WhenValidProductsProvided()
    {
        var (service, repo) = CreateSut();
        var command = new LoadProductsCommand(
        [
            new(Guid.NewGuid(), "Juice", 120, 5)
        ]);

        var result = await service.LoadProductsAsync(command);

        Assert.True(result.IsSuccess);
        await repo.Received(1).SaveAsync(Arg.Any<Domain.Entities.VendingMachine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadProductsAsync_ShouldReturnFailure_WhenListIsEmpty()
    {
        var (service, repo) = CreateSut();
        var command = new LoadProductsCommand([]);

        var result = await service.LoadProductsAsync(command);

        Assert.True(result.IsFailure);
        await repo.DidNotReceive().SaveAsync(Arg.Any<Domain.Entities.VendingMachine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadProductsAsync_ShouldReturnFailure_WhenPriceIsZero()
    {
        var (service, _) = CreateSut();
        var command = new LoadProductsCommand(
        [
            new(Guid.NewGuid(), "Juice", 0, 5)
        ]);

        var result = await service.LoadProductsAsync(command);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task LoadChangeAsync_ShouldReturnSuccess_WhenValidCoinsProvided()
    {
        var (service, repo) = CreateSut();
        var command = new LoadChangeCommand(new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.OnePound] = 5
        });

        var result = await service.LoadChangeAsync(command);

        Assert.True(result.IsSuccess);
        await repo.Received(1).SaveAsync(Arg.Any<Domain.Entities.VendingMachine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadChangeAsync_ShouldReturnFailure_WhenMapIsEmpty()
    {
        var (service, repo) = CreateSut();
        var command = new LoadChangeCommand(new Dictionary<CoinDenomination, int>());

        var result = await service.LoadChangeAsync(command);

        Assert.True(result.IsFailure);
        await repo.DidNotReceive().SaveAsync(Arg.Any<Domain.Entities.VendingMachine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMachineStateAsync_ShouldReturnAllProducts_WhenCalled()
    {
        var (service, _) = CreateSut();

        var state = await service.GetMachineStateAsync();

        Assert.Single(state.Products);
        Assert.Equal("Cola", state.Products[0].Name);
    }

    [Fact]
    public async Task GetAvailableProductsAsync_ShouldReturnOnlyInStockProducts_WhenCalled()
    {
        var (service, _) = CreateSut();

        var products = await service.GetAvailableProductsAsync();

        Assert.Single(products);
    }
}