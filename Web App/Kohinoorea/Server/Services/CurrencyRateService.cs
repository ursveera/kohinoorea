using Kohinoorea.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Kohinoorea.Server.Services;

public sealed class CurrencyRateService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly CurrencyPricingOptions _options;

    public CurrencyRateService(
        HttpClient http,
        IMemoryCache cache,
        IOptions<CurrencyPricingOptions> options)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<CurrencyRateDto> GetUsdRateAsync(string currencyCode)
    {
        currencyCode = string.IsNullOrWhiteSpace(currencyCode)
            ? "USD"
            : currencyCode.Trim().ToUpperInvariant();

        if (currencyCode == "USD")
        {
            return new CurrencyRateDto
            {
                CurrencyCode = "USD",
                RateFromUsd = 1m,
                Source = "base",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        // Exact production pricing override for INR
        if (currencyCode == "INR" &&
            _options.UseUsdToInrOverride &&
            _options.UsdToInrOverrideRate > 0)
        {
            return new CurrencyRateDto
            {
                CurrencyCode = "INR",
                RateFromUsd = _options.UsdToInrOverrideRate,
                Source = "configured-override",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        var cacheKey = $"usd-rate:{currencyCode}";

        if (_cache.TryGetValue(cacheKey, out CurrencyRateDto? cached) &&
            cached is not null)
        {
            return cached;
        }

        try
        {
            var response = await _http.GetFromJsonAsync<ExchangeRateApiResponse>(
                "https://open.er-api.com/v6/latest/USD");

            if (response?.Rates is not null &&
                response.Rates.TryGetValue(currencyCode, out var rate) &&
                rate > 0)
            {
                var dto = new CurrencyRateDto
                {
                    CurrencyCode = currencyCode,
                    RateFromUsd = rate,
                    Source = "open-er-api",
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                var cacheUntil = response.TimeNextUpdateUnix > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(response.TimeNextUpdateUnix)
                    : DateTimeOffset.UtcNow.AddHours(6);

                if (cacheUntil <= DateTimeOffset.UtcNow)
                {
                    cacheUntil = DateTimeOffset.UtcNow.AddHours(6);
                }

                _cache.Set(cacheKey, dto, cacheUntil);

                return dto;
            }
        }
        catch
        {
        }

        return new CurrencyRateDto
        {
            CurrencyCode = currencyCode,
            RateFromUsd = 1m,
            Source = "fallback",
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private sealed class ExchangeRateApiResponse
    {
        [JsonPropertyName("time_next_update_unix")]
        public long TimeNextUpdateUnix { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}