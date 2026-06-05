using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using VendingMachine.Application.DTOs;
using VendingMachine.Domain.Enums;

namespace VendingMachine.Api.Tests;

public sealed class VendingMachineApiTests : IDisposable
{
    private static readonly Guid ColaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VendingMachineApiTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetProducts_ShouldReturn200_WhenCalled()
    {
        var response = await _client.GetAsync("/api/vending-machine/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetState_ShouldReturn200_WhenCalled()
    {
        var response = await _client.GetAsync("/api/vending-machine/state");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnSeededProducts_WhenCalledInDevelopment()
    {
        var response = await _client.GetAsync("/api/vending-machine/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);

        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "Cola");
    }

    [Fact]
    public async Task Purchase_ShouldReturn200WithProduct_WhenExactAmountInserted()
    {
        await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 100 }); // OnePound
        await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 50 });  // FiftyPence

        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = ColaId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Cola", result.Product.Name);
    }

    [Fact]
    public async Task Purchase_ShouldReturn422_WhenInsufficientFunds()
    {
        await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 10 }); // TenPence

        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = ColaId
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_ShouldReturn422_WhenProductNotFound()
    {
        await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 200 }); // TwoPounds

        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task LoadProducts_ShouldReturn204_WhenValidProductsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/vending-machine/products/load", new
        {
            products = new[]
            {
                new { id = Guid.NewGuid(), name = "Test Snack", pricePence = 90, quantity = 3 }
            }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task LoadChange_ShouldReturn204_WhenValidCoinsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/vending-machine/change/load", new
        {
            coins = new Dictionary<string, int>
            {
                ["OnePound"] = 5,
                ["FiftyPence"] = 5
            }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_ShouldReturnChange_WhenOverpaymentProvided()
    {
        await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 200 }); // TwoPounds

        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = ColaId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseResultDto>(JsonOptions);
        Assert.NotNull(result);
        var changePence = result.Change.Sum(kv => (int)kv.Key * kv.Value);
        Assert.Equal(50, changePence);
    }

    [Fact]
    public async Task InsertCredit_ShouldReturn204_WhenValidAmountProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 100 }); // OnePound

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ReturnCredit_ShouldReturn200WithCoins_WhenCreditExists()
    {
        await _client.PostAsJsonAsync("/api/vending-machine/credit", new { denomination = 100 }); // OnePound

        var response = await _client.PostAsync("/api/vending-machine/credit/return", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<CoinDenomination, int>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(100, result.Sum(kv => (int)kv.Key * kv.Value));
    }
}