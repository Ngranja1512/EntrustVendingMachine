using Microsoft.AspNetCore.Mvc;
using VendingMachine.Application.Commands;
using VendingMachine.Application.DTOs;
using VendingMachine.Application.Interfaces;
using VendingMachine.Api.Models;
using VendingMachine.Domain.Enums;

namespace VendingMachine.Api.Controllers;

[ApiController]
[Route("api/vending-machine")]
public sealed class VendingMachineController : ControllerBase
{
    private readonly IVendingMachineService _service;

    public VendingMachineController(IVendingMachineService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    /// <summary>Purchases a product using currently inserted machine credit. Returns the product and any change due.</summary>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(PurchaseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Purchase([FromBody] PurchaseRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "ProductId must not be empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var command = new PurchaseProductCommand(request.ProductId);
        var result = await _service.PurchaseAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Purchase failed",
                Detail = result.Error,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        return Ok(result.Value);
    }

    /// <summary>Inserts money as user credit into the machine.</summary>
    [HttpPost("credit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> InsertCredit([FromBody] InsertCreditRequest request, CancellationToken cancellationToken)
    {
        var command = new InsertCreditCommand(request.Denomination);
        var result = await _service.InsertCreditAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Insert credit failed",
                Detail = result.Error,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        return NoContent();
    }

    /// <summary>Returns currently inserted user credit from the machine as change coins.</summary>
    [HttpPost("credit/return")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<CoinDenomination, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReturnCredit(CancellationToken cancellationToken)
    {
        var result = await _service.ReturnCreditAsync(cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Return credit failed",
                Detail = result.Error,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        return Ok(result.Value);
    }

    /// <summary>Loads or restocks products in the machine.</summary>
    [HttpPost("products/load")]00BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status4
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LoadProducts([FromBody] LoadProductsRequest request, CancellationToken cancellationToken)
    {
        var command = new LoadProductsCommand(
            request.Products
                .Select(p => new ProductToLoad(p.Id, p.Name, p.PricePence, p.Quantity))
                .ToList());

        var result = await _service.LoadProductsAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Load products failed",
                Detail = result.Error,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        return NoContent();
    }

    /// <summary>Loads coins into the machine's change float.</summary>
    [HttpPost("change/load")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LoadChange([FromBody] LoadChangeRequest request, CancellationToken cancellationToken)
    {
        var command = new LoadChangeCommand(request.Coins);
        var result = await _service.LoadChangeAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Load change failed",
                Detail = result.Error,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        return NoContent();
    }

    /// <summary>Returns the full current state of the machine including all products and the coin float.</summary>
    [HttpGet("state")]
    [ProducesResponseType(typeof(MachineStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetState(CancellationToken cancellationToken)
    {
        var state = await _service.GetMachineStateAsync(cancellationToken);
        return Ok(state);
    }

    /// <summary>Returns only products that are currently in stock.</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableProducts(CancellationToken cancellationToken)
    {
        var products = await _service.GetAvailableProductsAsync(cancellationToken);
        return Ok(products);
    }
}
