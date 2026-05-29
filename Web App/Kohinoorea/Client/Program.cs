using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Kohinoorea.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
var apiBaseAddress = builder.Configuration["ApiBaseAddress"];
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

var host = builder.Build();
var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
var httpClient = host.Services.GetRequiredService<HttpClient>();
var token = await localStorage.GetItemAsync<string>("authToken");

if (!string.IsNullOrWhiteSpace(token) && !IsTokenExpired(token))
{
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
else if (!string.IsNullOrWhiteSpace(token))
{
    await localStorage.RemoveItemAsync("authToken");
    await localStorage.RemoveItemAsync("authLastActivityUtc");
}

await host.RunAsync();

static bool IsTokenExpired(string token)
{
    var handler = new JwtSecurityTokenHandler();
    if (!handler.CanReadToken(token))
    {
        return true;
    }

    var jwt = handler.ReadJwtToken(token);
    return jwt.ValidTo <= DateTime.UtcNow;
}
