using System.Security.Claims;
using Kohinoorea.Server.Options;
using Kohinoorea.Server.Services;
using Kohinoorea.Shared.Models.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class StripeController : ControllerBase
{
    private readonly ICommerceRepository _commerceRepository;
    private readonly IOptions<StripeOptions> _stripeOptions;

    public StripeController(ICommerceRepository commerceRepository, IOptions<StripeOptions> stripeOptions)
    {
        _commerceRepository = commerceRepository;
        _stripeOptions = stripeOptions;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CreateStripeCheckoutResponse>> CreateCheckout([FromBody] CreateStripeCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var items = request.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .Select(i => new StripeCheckoutItem { ProductId = i.ProductId, Quantity = Math.Min(i.Quantity, 10) })
            .ToList();

        if (items.Count == 0)
        {
            return BadRequest("No items provided.");
        }

        var stripeOptions = _stripeOptions.Value ?? new StripeOptions();
        var (secretKey, _) = ResolveStripeSecrets(stripeOptions);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Stripe is not configured.");
        }

        StripeConfiguration.ApiKey = secretKey;

        var lineItems = new List<SessionLineItemOptions>();
        var createdOrderIds = new List<long>();

        foreach (var item in items)
        {
            var product = await _commerceRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
            {
                return NotFound($"Product {item.ProductId} not found.");
            }

            // Create a pending order immediately; webhook will set Completed/Failed.
            var orderId = await _commerceRepository.CreateOrderAsync(userId, product, item.Quantity, paymentMethod: "Stripe", cancellationToken: cancellationToken);
            createdOrderIds.Add(orderId);

            var unitAmount = (long)Math.Round(product.Price * 100m, MidpointRounding.AwayFromZero);
            lineItems.Add(new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = unitAmount,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = product.Name,
                        Description = string.IsNullOrWhiteSpace(product.Description)
                            ? null
                            : (product.Description.Length > 200 ? product.Description[..200] : product.Description)
                    }
                }
            });
        }

        var successUrl = string.IsNullOrWhiteSpace(stripeOptions.SuccessUrl)
            ? "https://kohinoorea.com/dashboard#orders"
            : stripeOptions.SuccessUrl!;
        var cancelUrl = string.IsNullOrWhiteSpace(stripeOptions.CancelUrl)
            ? "https://kohinoorea.com/cart"
            : stripeOptions.CancelUrl!;

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = userId.ToString(),
            LineItems = lineItems,
            Metadata = new Dictionary<string, string>
            {
                ["orderIds"] = string.Join(",", createdOrderIds),
                ["userId"] = userId.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        return Ok(new CreateStripeCheckoutResponse
        {
            CheckoutUrl = session.Url ?? string.Empty
        });
    }

    private static (string? SecretKey, string? WebhookSecret) ResolveStripeSecrets(StripeOptions stripeOptions)
    {
        var mode = (stripeOptions.Mode ?? "Test").Trim();
        var isLive = string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase);

        return isLive
            ? (stripeOptions.LiveSecretKey, stripeOptions.LiveWebhookSecret)
            : (stripeOptions.TestSecretKey, stripeOptions.TestWebhookSecret);
    }
}
