using Blazored.LocalStorage;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kohinoorea.Client.Services;

public sealed class CurrencyPricingOptions
{
    public bool UseUsdToInrOverride { get; set; }
    public decimal UsdToInrOverrideRate { get; set; }
}

public sealed class CurrencyRateDto
{
    public string CurrencyCode { get; set; } = "USD";
    public decimal RateFromUsd { get; set; } = 1m;
    public string Source { get; set; } = "fallback";
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CurrencyContext
{
    private const string CountryCacheKey = "visitorCountryCode";
    private const string CurrencyCacheKey = "visitorCurrencyCode";
    private const string RateCacheKey = "visitorRateFromUsd";
    private const string RateCacheTimeKey = "visitorRateUpdatedUtc";

    private static readonly TimeSpan RateCacheDuration = TimeSpan.FromMinutes(15);

    public string CountryCode { get; init; } = "US";
    public string CurrencyCode { get; init; } = "USD";
    public decimal RateFromUsd { get; init; } = 1m;
    public CultureInfo Culture { get; init; } = CultureInfo.GetCultureInfo("en-US");

    public decimal ConvertFromUsd(decimal usdAmount) => usdAmount * RateFromUsd;

    public string FormatFromUsd(decimal usdAmount, string format = "N0")
    {
        var amount = ConvertFromUsd(usdAmount);
        var number = amount.ToString(format, Culture);
        var symbol = Culture.NumberFormat.CurrencySymbol;
        return $"{symbol}{number}";
    }

    public static string FormatPriceForCountry(decimal amount, string? countryCode, string format = "N0")
    {
        var normalizedCountry = string.IsNullOrWhiteSpace(countryCode)
            ? "US"
            : countryCode.Trim().ToUpperInvariant();

        var currencyCode = GetCurrencyCodeFromCountry(normalizedCountry);
        var culture = GetCulture(normalizedCountry, currencyCode);
        var number = amount.ToString(format, culture);
        var symbol = culture.NumberFormat.CurrencySymbol;
        return $"{symbol}{number}";
    }

    public string ReplacePricesInTextFromUsd(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Replace tokens like "$99", "$99.00", "99.00", "USD 99.00", "INR 99.00", "₹99"
        // Assumption: stored product prices are USD base amounts.
        return Regex.Replace(
            input,
            @"(?ix)
              (?:
                (?<sym>\$|₹)\s*(?<amt1>\d+(?:\.\d{2})?) |
                (?:(?:USD|INR|Rs\.?)\s*)?(?<amt2>\d+\.\d{2})
              )",
            match =>
            {
                var amountText = match.Groups["amt1"].Success
                    ? match.Groups["amt1"].Value
                    : match.Groups["amt2"].Value;

                if (!decimal.TryParse(
                        amountText,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var usd))
                {
                    return match.Value;
                }

                // Avoid converting percentages like "33.33%" or "$33.33%".
                var nextIndex = match.Index + match.Length;
                if (nextIndex < input.Length && input[nextIndex] == '%')
                {
                    return match.Value;
                }

                // Preserve cents when explicitly present.
                var hasCents = amountText.Contains('.', StringComparison.Ordinal);
                return FormatFromUsd(usd, hasCents ? "N2" : "N0");
            });
    }

    private sealed class IpapiResponse
    {
        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("currency")]
        public string? CurrencyCode { get; set; }
    }

    private sealed class CurrencyRateApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("rateFromUsd")]
        public decimal RateFromUsd { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    public static async Task<CurrencyContext> ResolveAsync(
        HttpClient http,
        ILocalStorageService localStorage)
    {
        // 1) Try cached country/currency/rate
        var cachedCountry = await localStorage.GetItemAsync<string>(CountryCacheKey);
        var cachedCurrency = await localStorage.GetItemAsync<string>(CurrencyCacheKey);
        var cachedRate = await localStorage.GetItemAsync<decimal?>(RateCacheKey);
        var cachedRateTime = await localStorage.GetItemAsync<DateTime?>(RateCacheTimeKey);

        var hasFreshCachedRate =
            !string.IsNullOrWhiteSpace(cachedCountry) &&
            !string.IsNullOrWhiteSpace(cachedCurrency) &&
            cachedRate.HasValue &&
            cachedRate.Value > 0 &&
            cachedRateTime.HasValue &&
            DateTime.UtcNow - cachedRateTime.Value <= RateCacheDuration;

        if (hasFreshCachedRate)
        {
            return CreateContext(
                cachedCountry!,
                cachedCurrency!,
                cachedRate!.Value);
        }

        // 2) Detect country and currency from visitor IP
        var detectedCountry = "US";
        var detectedCurrency = "USD";

        try
        {
            var geo = await http.GetFromJsonAsync<IpapiResponse>(
                "https://ipapi.co/json/");

            if (!string.IsNullOrWhiteSpace(geo?.CountryCode))
            {
                detectedCountry = geo.CountryCode.Trim().ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(geo?.CurrencyCode))
            {
                detectedCurrency = geo.CurrencyCode.Trim().ToUpperInvariant();
            }
        }
        catch
        {
            // Best-effort geo lookup.
            // Keep US/USD fallback.
        }

        // 3) Fetch live USD -> detected currency rate
        var rateFromUsd = await GetLiveUsdRateAsync(http, detectedCurrency);

        // 4) If API fails, try stale cached rate for same currency
        if (rateFromUsd <= 0 &&
            cachedRate.HasValue &&
            cachedRate.Value > 0 &&
            string.Equals(cachedCurrency, detectedCurrency, StringComparison.OrdinalIgnoreCase))
        {
            rateFromUsd = cachedRate.Value;
        }

        // 5) Final safe fallback
        if (rateFromUsd <= 0)
        {
            rateFromUsd = detectedCurrency == "USD" ? 1m : 1m;
        }

        // 6) Cache resolved values
        await localStorage.SetItemAsync(CountryCacheKey, detectedCountry);
        await localStorage.SetItemAsync(CurrencyCacheKey, detectedCurrency);
        await localStorage.SetItemAsync(RateCacheKey, rateFromUsd);
        await localStorage.SetItemAsync(RateCacheTimeKey, DateTime.UtcNow);

        return CreateContext(
            detectedCountry,
            detectedCurrency,
            rateFromUsd);
    }

    public static CurrencyContext FromCountryCode(string countryCode)
    {
        var code = string.IsNullOrWhiteSpace(countryCode)
            ? "US"
            : countryCode.Trim().ToUpperInvariant();

        var currencyCode = GetCurrencyCodeFromCountry(code);

        // This method remains synchronous, so it cannot fetch a live rate.
        // Live rates are resolved in ResolveAsync().
        return CreateContext(
            code,
            currencyCode,
            currencyCode == "USD" ? 1m : 1m);
    }

    private static async Task<decimal> GetLiveUsdRateAsync(
    HttpClient http,
    string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return 1m;
        }

        currencyCode = currencyCode.Trim().ToUpperInvariant();

        if (currencyCode == "USD")
        {
            return 1m;
        }

        try
        {
            var response = await http.GetFromJsonAsync<CurrencyRateApiResponse>(
                $"/api/currency/rate/{currencyCode}");

            if (response is not null &&
                response.RateFromUsd > 0)
            {
                return response.RateFromUsd;
            }
        }
        catch
        {
        }

        return 0m;
    }

    private static CurrencyContext CreateContext(
        string countryCode,
        string currencyCode,
        decimal rateFromUsd)
    {
        return new CurrencyContext
        {
            CountryCode = countryCode,
            CurrencyCode = currencyCode,
            RateFromUsd = rateFromUsd <= 0 ? 1m : rateFromUsd,
            Culture = GetCulture(countryCode, currencyCode)
        };
    }

    private static string GetCurrencyCodeFromCountry(string countryCode)
    {
        return countryCode switch
        {
            "IN" => "INR",
            "US" => "USD",
            "GB" => "GBP",
            "AE" => "AED",
            "CA" => "CAD",
            "AU" => "AUD",
            "SG" => "SGD",
            "JP" => "JPY",
            "DE" => "EUR",
            "FR" => "EUR",
            "IT" => "EUR",
            "ES" => "EUR",
            _ => "USD"
        };
    }

    private static CultureInfo GetCulture(
        string countryCode,
        string currencyCode)
    {
        var cultureName = (countryCode, currencyCode) switch
        {
            ("IN", "INR") => "en-IN",
            ("US", "USD") => "en-US",
            ("GB", "GBP") => "en-GB",
            ("AE", "AED") => "ar-AE",
            ("CA", "CAD") => "en-CA",
            ("AU", "AUD") => "en-AU",
            ("SG", "SGD") => "en-SG",
            ("JP", "JPY") => "ja-JP",
            ("DE", "EUR") => "de-DE",
            ("FR", "EUR") => "fr-FR",
            ("IT", "EUR") => "it-IT",
            ("ES", "EUR") => "es-ES",
            _ => "en-US"
        };

        return CultureInfo.GetCultureInfo(cultureName);
    }
}
