using VendingMachine.Application;
using VendingMachine.Application.Commands;
using VendingMachine.Application.Services;
using VendingMachine.Domain.Enums;
using VendingMachine.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// Seed the machine with initial products and change in development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<VendingMachineService>();

    await service.LoadProductsAsync(new LoadProductsCommand(
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Cola",      150, 10),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Crisps",    100, 10),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Water",      80, 10),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Chocolate", 125, 10)
    ]));

    await service.LoadChangeAsync(new LoadChangeCommand(
        new Dictionary<CoinDenomination, int>
        {
            [CoinDenomination.OnePence]    = 20,
            [CoinDenomination.TwoPence]    = 20,
            [CoinDenomination.FivePence]   = 20,
            [CoinDenomination.TenPence]    = 20,
            [CoinDenomination.TwentyPence] = 20,
            [CoinDenomination.FiftyPence]  = 20,
            [CoinDenomination.OnePound]    = 20,
            [CoinDenomination.TwoPounds]   = 20
        }));
}

app.Run();

// Required for WebApplicationFactory in integration tests.
public partial class Program { }
