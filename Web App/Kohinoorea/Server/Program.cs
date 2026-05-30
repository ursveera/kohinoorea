using Dapper;
using Kohinoorea.Client.Services;
using Kohinoorea.Server.Options;
using Kohinoorea.Server.Middleware;
using Kohinoorea.Server.Services;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth.OAuth2;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Text;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

// QuestPDF (community license)
try
{
    QuestPDF.Settings.License = LicenseType.Community;
}
catch
{
    // Hosting may run in an unsupported runtime (e.g., win-x86 / 32-bit),
    // causing QuestPDF native dependencies to fail to load.
    // The app can still run without PDF export support.
}

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                {
                    return false;
                }

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                // Allow known hosts across http/https and varying ports (needed for IIS/Azure/AppService + local dev).
                var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "localhost",
                    "kohinoorea.com",
                    "www.kohinoorea.com",
                    "dev-kohinoorea.kohinoorea.com",
                    "apikohinoorea.kohinoorea.com",
                    "dev-api-kohinoorea.kohinoorea.com"
                };

                return allowedHosts.Contains(uri.Host);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<SqlConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<QueryFactory>(sp =>
    new QueryFactory(sp.GetRequiredService<SqlConnection>(), new SqlServerCompiler()));
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ICommerceRepository, CommerceRepository>();
builder.Services.AddScoped<IEmailDeliveryService, SmtpEmailDeliveryService>();
builder.Services.AddScoped<IOrderEmailDeliveryService>(sp =>
    new SmtpEmailDeliveryService(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<SmtpEmailDeliveryService>>(),
        sp.GetRequiredService<IHostEnvironment>(),
        settingsPrefix: "Orders:Smtp"));
builder.Services.AddScoped<ISupportEmailDeliveryService>(sp =>
    new SmtpEmailDeliveryService(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<SmtpEmailDeliveryService>>(),
        sp.GetRequiredService<IHostEnvironment>(),
        settingsPrefix: "Support:Smtp"));
builder.Services.AddScoped<IAdminEmailDeliveryService>(sp =>
    new SmtpEmailDeliveryService(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<SmtpEmailDeliveryService>>(),
        sp.GetRequiredService<IHostEnvironment>(),
        settingsPrefix: "Admin:Smtp"));
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddSingleton<IPasswordHasher<string>, PasswordHasher<string>>();

builder.Services.Configure<FirebaseOptions>(
    builder.Configuration.GetSection("Firebase"));

builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.Configure<PaymentOptions>(
    builder.Configuration.GetSection("Payment"));

builder.Services.Configure<PayPalOptions>(
    builder.Configuration.GetSection("PayPal"));

builder.Services.Configure<CurrencyPricingOptions>(
    builder.Configuration.GetSection("CurrencyPricing"));

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<CurrencyRateService>();
builder.Services.AddHttpClient<PayPalCheckoutService>();
var app = builder.Build();

// Firebase Admin SDK (used to mint custom tokens for client-side Firebase Auth)
try
{
    var firebaseOptions = builder.Configuration.GetSection("Firebase").Get<FirebaseOptions>() ?? new FirebaseOptions();
    var serviceAccountPath = firebaseOptions.ServiceAccountPath ?? Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_PATH");
    if (!string.IsNullOrWhiteSpace(serviceAccountPath))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(serviceAccountPath),
            ProjectId = firebaseOptions.ProjectId
        });
    }
}
catch
{
    // Best-effort: app still runs without Firebase, but /api/firebase/token will fail.
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("ClientCors");

// CORS must run before global exception handling writes responses,
// otherwise error responses can miss CORS headers and fail in browsers.
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/currency/rate/{currencyCode}",
    async (
        string currencyCode,
        CurrencyRateService currencyRateService) =>
    {
        var result = await currencyRateService.GetUsdRateAsync(currencyCode);
        return Results.Ok(result);
    });

app.Run();
