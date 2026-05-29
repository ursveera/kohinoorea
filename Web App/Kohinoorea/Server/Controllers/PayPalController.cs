using Kohinoorea.Server.Options;
using Kohinoorea.Server.Services;
using Kohinoorea.Shared.Models.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/paypal")]
public sealed class PayPalController : ControllerBase
{
    private readonly PayPalCheckoutService _payPalCheckoutService;
    private readonly ICommerceRepository _commerceRepository;
    private readonly IOptions<PaymentOptions> _paymentOptions;
    private readonly IMemoryCache _memoryCache;

    public PayPalController(
        PayPalCheckoutService payPalCheckoutService,
        ICommerceRepository commerceRepository,
        IOptions<PaymentOptions> paymentOptions,
        IMemoryCache memoryCache)
    {
        _payPalCheckoutService = payPalCheckoutService;
        _commerceRepository = commerceRepository;
        _paymentOptions = paymentOptions;
        _memoryCache = memoryCache;
    }

    [HttpPost("capture")]
    public async Task<IActionResult> Capture([FromBody] CapturePayPalOrderRequest request, CancellationToken cancellationToken)
    {
        var provider = (_paymentOptions.Value?.Provider ?? "Stripe").Trim();
        if (!string.Equals(provider, "PayPal", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("PayPal provider is not enabled.");
        }

        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return BadRequest("OrderId is required.");
        }

        var orderId = request.OrderId.Trim();
        var status = await _payPalCheckoutService.CaptureOrderAsync(orderId, cancellationToken);

        // Update mapped SQL orders (created during checkout) based on capture result.
        if (_memoryCache.TryGetValue($"paypal:order:{orderId}", out List<long>? mappedOrderIds) && mappedOrderIds is { Count: > 0 })
        {
            var newStatus = string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                ? "Completed"
                : "Failed";

            foreach (var mapped in mappedOrderIds)
            {
                await _commerceRepository.UpdateOrderStatusAsync(mapped, newStatus, cancellationToken);
            }
        }

        return Ok(new { Status = status });
    }
}

