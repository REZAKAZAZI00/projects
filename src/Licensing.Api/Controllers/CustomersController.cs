using Licensing.Application.Abstractions;
using Licensing.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = "Admin")]
public class CustomersController : ControllerBase
{
    private readonly ILicensingDbContext _db;

    public CustomersController(ILicensingDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.ExternalCustomerId, c.Name, c.Email })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalCustomerId = request.ExternalCustomerId,
            Name = request.Name,
            Email = request.Email,
            MetadataJson = request.MetadataJson,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { id = customer.Id }, customer);
    }

    public record CreateCustomerRequest(string ExternalCustomerId, string Name, string Email, string? MetadataJson);
}
