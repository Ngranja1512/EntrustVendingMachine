# Entrust Vending Machine

A production-ready vending machine REST API built with .NET 10 and Clean Architecture.

## Architecture

```
Domain → Application → Infrastructure → API
```

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `VendingMachine.Domain` | Entities, value objects, domain services, `Result<T>` |
| Application | `VendingMachine.Application` | Use cases, commands, DTOs, repository interface |
| Infrastructure | `VendingMachine.Infrastructure` | In-memory repository with `SemaphoreSlim` thread safety |
| API | `VendingMachine.Api` | Controllers, request models, OpenAPI, DI wiring |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Running the API

```bash
dotnet run --project src/VendingMachine.Api
```

The API starts at `https://localhost:5001` / `http://localhost:5000`.  
OpenAPI docs: `http://localhost:5000/openapi`

In `Development` environment, seed data is loaded automatically:

| Product | Price | Stock |
|---|---|---|
| Cola | £1.50 | 10 |
| Crisps | £1.00 | 10 |
| Water | £0.80 | 10 |
| Chocolate | £1.25 | 10 |

20 coins of each denomination (1p–£2) are pre-loaded as change.

## Running Tests

```bash
dotnet test
```

| Test project | Type | Count |
|---|---|---|
| `VendingMachine.Domain.Tests` | Unit | 28 |
| `VendingMachine.Application.Tests` | Unit | 11 |
| `VendingMachine.Api.Tests` | Integration | 11 |

## API Reference

### `GET /api/vending-machine/products`
Returns all in-stock products.

### `GET /api/vending-machine/state`
Returns full machine state — all products and coin float.

### `POST /api/vending-machine/purchase`
Purchase a product.

```json
{
  "productId": "11111111-1111-1111-1111-111111111111",
  "amountInsertedPence": 200
}
```

Returns `200 OK` with the dispensed product and change, or `422 Unprocessable Entity` on failure (insufficient funds, out of stock, cannot make change).

### `POST /api/vending-machine/products/load`
Restock products.

```json
{
  "products": [
    { "id": "11111111-1111-1111-1111-111111111111", "name": "Cola", "pricePence": 150, "quantity": 10 }
  ]
}
```

### `POST /api/vending-machine/change/load`
Load coins into the change float.

```json
{
  "coins": {
    "OnePound": 10,
    "FiftyPence": 20,
    "TwentyPence": 20
  }
}
```

Valid denomination keys: `OnePence`, `TwoPence`, `FivePence`, `TenPence`, `TwentyPence`, `FiftyPence`, `OnePound`, `TwoPounds`.

## Key Design Decisions

| Decision | Choice |
|---|---|
| Error handling | `Result<T>` for domain failures; exceptions only for programming errors |
| Currency precision | All amounts stored in pence (integer) to avoid floating-point errors |
| Change algorithm | Greedy — largest denomination first |
| Concurrency | `SemaphoreSlim` in repository for async-safe locking |
| Mocking | NSubstitute |
| State | In-memory singleton; swap `IVendingMachineRepository` for EF Core without changing other layers |
