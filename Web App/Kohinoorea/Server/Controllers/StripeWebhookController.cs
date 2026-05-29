using System.Text;
using Kohinoorea.Server.Options;
using Kohinoorea.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly ICommerceRepository _commerceRepository;
    private readonly IOptions<StripeOptions> _stripeOptions;

    public StripeWebhookController(ICommerceRepository commerceRepository, IOptions<StripeOptions> stripeOptions)
    {
        _commerceRepository = commerceRepository;
        _stripeOptions = stripeOptions;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        var stripeOptions = _stripeOptions.Value ?? new StripeOptions();
        var secretsToTry = new[]
        {
            stripeOptions.TestWebhookSecret,
            stripeOptions.LiveWebhookSecret
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

        if (secretsToTry.Count == 0)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Stripe webhook is not configured.");
        }

        Event? stripeEvent = null;
        foreach (var secret in secretsToTry)
        {
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, secret!);
                break;
            }
            catch
            {
                // Try next secret
            }
        }

        if (stripeEvent is null)
        {
            return BadRequest();
        }

        if (string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.OrdinalIgnoreCase))
        {
            var session = stripeEvent.Data.Object as Session;
            await UpdateOrdersFromSessionAsync(session, "Completed", cancellationToken);
        }
        else if (string.Equals(stripeEvent.Type, "checkout.session.expired", StringComparison.OrdinalIgnoreCase))
        {
            var session = stripeEvent.Data.Object as Session;
            await UpdateOrdersFromSessionAsync(session, "Failed", cancellationToken);
        }

        return Ok();
    }

    private async Task UpdateOrdersFromSessionAsync(Session? session, string status, CancellationToken cancellationToken)
    {
        if (session?.Metadata is null)
        {
            return;
        }

        if (!session.Metadata.TryGetValue("orderIds", out var orderIdsValue) || string.IsNullOrWhiteSpace(orderIdsValue))
        {
            return;
        }

        var ids = orderIdsValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => long.TryParse(v, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        foreach (var orderId in ids)
        {
            await _commerceRepository.UpdateOrderStatusAsync(orderId, status, cancellationToken);
        }
    }
}
