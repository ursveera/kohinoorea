using System.Security.Claims;
using Kohinoorea.Server.Options;
using Kohinoorea.Server.Services;
using Kohinoorea.Shared.Models.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ICommerceRepository _commerceRepository;
    private readonly PayPalCheckoutService _payPalCheckoutService;
    private readonly IOptions<PaymentOptions> _paymentOptions;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<StripeOptions> _stripeOptions;

    public PaymentsController(
        ICommerceRepository commerceRepository,
        PayPalCheckoutService payPalCheckoutService,
        IOptions<PaymentOptions> paymentOptions,
        IMemoryCache memoryCache,
        IOptions<StripeOptions> stripeOptions)
    {
        _commerceRepository = commerceRepository;
        _payPalCheckoutService = payPalCheckoutService;
        _paymentOptions = paymentOptions;
        _memoryCache = memoryCache;
        _stripeOptions = stripeOptions;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CreateCheckoutResponse>> Checkout(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
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

        var provider = string.IsNullOrWhiteSpace(request.Provider)
            ? (_paymentOptions.Value?.Provider ?? "Stripe").Trim()
            : request.Provider.Trim();
        if (string.Equals(provider, "PayPal", StringComparison.OrdinalIgnoreCase))
        {
            return await CreatePayPalCheckoutAsync(userId, request, cancellationToken);
        }

        // Default: Stripe
        return await CreateStripeCheckoutAsync(userId, request, cancellationToken);
    }

    private async Task<ActionResult<CreateCheckoutResponse>> CreateStripeCheckoutAsync(
        long userId,
        CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
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

        return Ok(new CreateCheckoutResponse
        {
            Provider = "Stripe",
            CheckoutUrl = session.Url ?? string.Empty
        });
    }

    private async Task<ActionResult<CreateCheckoutResponse>> CreatePayPalCheckoutAsync(
        long userId,
        CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .Select(i => new { i.ProductId, Quantity = Math.Min(i.Quantity, 10) })
            .ToList();

        if (items.Count == 0)
        {
            return BadRequest("No items provided.");
        }

        decimal totalUsd = 0m;
        foreach (var item in items)
        {
            var product = await _commerceRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
            {
                return NotFound($"Product {item.ProductId} not found.");
            }

            totalUsd += product.Price * item.Quantity;
        }

        var orderIds = new List<long>();
        foreach (var item in items)
        {
            var product = await _commerceRepository.GetProductByIdAsync(item.ProductId, cancellationToken);
            if (product is null)
            {
                continue;
            }

            var orderId = await _commerceRepository.CreateOrderAsync(
                userId,
                product,
                item.Quantity,
                paymentMethod: "PayPal",
                cancellationToken: cancellationToken);
            orderIds.Add(orderId);
        }

        var reference = string.Join(",", orderIds);
        var (payPalOrderId, approveUrl) = await _payPalCheckoutService.CreateOrderAsync(totalUsd, reference, cancellationToken);

        if (string.IsNullOrWhiteSpace(approveUrl))
        {
            return StatusCode(StatusCodes.Status502BadGateway, "PayPal did not return an approval URL.");
        }

        // Store mapping so we can update our SQL orders after capture.
        // (Short TTL is fine; PayPal approval/capture is usually immediate.)
        _memoryCache.Set(
            $"paypal:order:{payPalOrderId}",
            orderIds,
            new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(2) });

        return Ok(new CreateCheckoutResponse
        {
            Provider = "PayPal",
            CheckoutUrl = approveUrl
        });
    }

    private sealed class StripeCheckoutItem
    {
        public long ProductId { get; init; }
        public int Quantity { get; init; }
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
