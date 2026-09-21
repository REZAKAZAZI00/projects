using Licensing.Api.Extensions;
using Licensing.Application.Models;
using Licensing.Application.Services;
using Licensing.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Authorize(Roles = "Admin")]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionsController(SubscriptionService subscriptionService) => _subscriptionService = subscriptionService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.CreateAsync(request, User.GetActor(), HttpContext.GetClientIp(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _subscriptionService.GetAsync(id, cancellationToken);
        return item is null ? NotFound(new { error = "Subscription not found." }) : Ok(item);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await _subscriptionService.GetHistoryAsync(id, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SubscriptionQuery query, CancellationToken cancellationToken) =>
        Ok(await _subscriptionService.ListAsync(query, cancellationToken));

    [HttpPost("{id:guid}/renew")]
    public async Task<IActionResult> Renew(Guid id, [FromBody] RenewSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await _subscriptionService.RenewAsync(id, request, User.GetActor(), HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest request, CancellationToken cancellationToken)
    {
        await _subscriptionService.CancelAsync(id, User.GetActor(), request.Reason, HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/grace-period")]
    public async Task<IActionResult> GracePeriod(Guid id, [FromBody] GracePeriodRequest request, CancellationToken cancellationToken)
    {
        await _subscriptionService.SetGracePeriodAsync(id, request.GraceDays, User.GetActor(), HttpContext.GetClientIp(), cancellationToken);
        return NoContent();
    }

    public record CancelRequest(string? Reason);
    public record GracePeriodRequest(int GraceDays);
}
