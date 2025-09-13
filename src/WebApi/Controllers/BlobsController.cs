using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RekazDrive.Application.Services;

namespace RekazDrive.WebApi.Controllers;

[ApiController]
[Route("v1/blobs")]
[Authorize]
public sealed class BlobsController : ControllerBase
{
    private readonly BlobService _service;
    public BlobsController(BlobService service) => _service = service;

    public sealed record StoreRequest(string Data);

    [HttpPost]
    public async Task<IActionResult> Store([FromBody] StoreRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Data)) return BadRequest("data is required");
        try
        {
            var result = await _service.StoreAsync(request.Data, ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, new
            {
                id = result.Id,
                size = result.Size,
                created_at = result.CreatedAt.ToUniversalTime().ToString("O")
            });
        }
        catch (FormatException)
        {
            return BadRequest("data must be a valid Base64 string");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            var res = await _service.RetrieveAsync(id, ct);
            return Ok(new
            {
                id = res.Id,
                data = res.Data,
                size = res.Size,
                created_at = res.CreatedAt.ToUniversalTime().ToString("O")
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
