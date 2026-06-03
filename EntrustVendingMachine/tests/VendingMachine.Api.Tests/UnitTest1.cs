using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using VendingMachine.Application.DTOs;
using VendingMachine.Domain.Enums;

namespace VendingMachine.Api.Tests;

public sealed class VendingMachineApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid ColaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VendingMachineApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
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
        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = ColaId,
            amountInsertedPence = 150
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Cola", result.Product.Name);
    }

    [Fact]
    public async Task Purchase_ShouldReturn422_WhenInsufficientFunds()
    {
        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = ColaId,
            amountInsertedPence = 10
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_ShouldReturn422_WhenProductNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = Guid.NewGuid(),
            amountInsertedPence = 200
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
        var response = await _client.PostAsJsonAsync("/api/vending-machine/purchase", new
        {
            productId = ColaId,
            amountInsertedPence = 200
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseResultDto>(JsonOptions);
        Assert.NotNull(result);
        var changePence = result.Change.Sum(kv => (int)kv.Key * kv.Value);
        Assert.Equal(50, changePence);
    }
}