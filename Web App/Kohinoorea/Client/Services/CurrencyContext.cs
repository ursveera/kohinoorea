using Blazored.LocalStorage;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Kohinoorea.Client.Services;

public sealed class CurrencyContext
{
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
                var amountText = match.Groups["amt1"].Success ? match.Groups["amt1"].Value : match.Groups["amt2"].Value;
                if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var usd))
                {
                    return match.Value;
                }

                // Avoid converting percentages like "33.33%" or "$33.33%".
                // If the original match is immediately followed by a percent sign, keep as-is.
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
        [System.Text.Json.Serialization.JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }

    public static async Task<CurrencyContext> ResolveAsync(HttpClient http, ILocalStorageService localStorage)
    {
        // 1) Read cached context
        var cachedCountry = await localStorage.GetItemAsync<string>("visitorCountryCode");
        if (!string.IsNullOrWhiteSpace(cachedCountry))
        {
            return FromCountryCode(cachedCountry);
        }

        // 2) Try geo lookup (best-effort)
        string? detectedCountry = null;
        try
        {
            var geo = await http.GetFromJsonAsync<IpapiResponse>("https://ipapi.co/json/");
            detectedCountry = geo?.CountryCode;
        }
        catch
        {
        }

        detectedCountry = string.IsNullOrWhiteSpace(detectedCountry) ? "US" : detectedCountry.Trim().ToUpperInvariant();
        await localStorage.SetItemAsync("visitorCountryCode", detectedCountry);
        return FromCountryCode(detectedCountry);
    }

    public static CurrencyContext FromCountryCode(string countryCode)
    {
        var code = string.IsNullOrWhiteSpace(countryCode) ? "US" : countryCode.Trim().ToUpperInvariant();

        // NOTE: Prices in DB are treated as USD base amounts.
        // Add more countries/rates here as needed.
        return code switch
        {
            "IN" => new CurrencyContext
            {
                CountryCode = "IN",
                CurrencyCode = "INR",
                // Keep it simple/configurable later: USD -> INR approximate.
                RateFromUsd = 83m,
                Culture = CultureInfo.GetCultureInfo("en-IN")
            },
            _ => new CurrencyContext
            {
                CountryCode = "US",
                CurrencyCode = "USD",
                RateFromUsd = 1m,
                Culture = CultureInfo.GetCultureInfo("en-US")
            }
        };
    }
}
