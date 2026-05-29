using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Kohinoorea.Server.Options;
using Microsoft.Extensions.Options;

namespace Kohinoorea.Server.Services;

public sealed class PayPalCheckoutService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<PayPalOptions> _payPalOptions;

    public PayPalCheckoutService(HttpClient httpClient, IOptions<PayPalOptions> payPalOptions)
    {
        _httpClient = httpClient;
        _payPalOptions = payPalOptions;
    }

    public (string BaseUrl, string ClientId, string ClientSecret, string SuccessUrl, string CancelUrl) GetConfig()
    {
        var options = _payPalOptions.Value ?? new PayPalOptions();
        var mode = (options.Mode ?? "Test").Trim();
        var isLive = string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase);

        var baseUrl = isLive ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com";
        var clientId = isLive ? (options.LiveClientId ?? string.Empty) : (options.TestClientId ?? string.Empty);
        var clientSecret = isLive ? (options.LiveClientSecret ?? string.Empty) : (options.TestClientSecret ?? string.Empty);

        var successUrl = string.IsNullOrWhiteSpace(options.SuccessUrl)
            ? "https://kohinoorea.com/dashboard#orders"
            : options.SuccessUrl!;
        var cancelUrl = string.IsNullOrWhiteSpace(options.CancelUrl)
            ? "https://kohinoorea.com/cart"
            : options.CancelUrl!;

        return (baseUrl, clientId, clientSecret, successUrl, cancelUrl);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var (baseUrl, clientId, clientSecret, _, _) = GetConfig();
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("PayPal is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}")));
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
    }

    public async Task<(string OrderId, string ApproveUrl)> CreateOrderAsync(
        decimal amountUsd,
        string referenceId,
        CancellationToken cancellationToken)
    {
        var (baseUrl, _, _, successUrl, cancelUrl) = GetConfig();
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var currencyCode = (_payPalOptions.Value?.CurrencyCode ?? "USD").Trim();
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            currencyCode = "USD";
        }

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = referenceId,
                    custom_id = referenceId,
                    amount = new { currency_code = currencyCode.ToUpperInvariant(), value = amountUsd.ToString("0.00") }
                }
            },
            application_context = new
            {
                user_action = "PAY_NOW",
                return_url = successUrl,
                cancel_url = cancelUrl
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var orderId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;

        var approveUrl = string.Empty;
        if (doc.RootElement.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                var rel = link.GetProperty("rel").GetString();
                if (string.Equals(rel, "approve", StringComparison.OrdinalIgnoreCase))
                {
                    approveUrl = link.GetProperty("href").GetString() ?? string.Empty;
                    break;
                }
            }
        }

        return (orderId, approveUrl);
    }

    public async Task<string> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        var (baseUrl, _, _, _, _) = GetConfig();
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders/{orderId}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("status").GetString() ?? string.Empty;
    }
}
