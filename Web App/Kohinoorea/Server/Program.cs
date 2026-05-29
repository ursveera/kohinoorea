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

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy
            .WithOrigins(
            "http://localhost:5010",
            "https://localhost:5010",
            "https://localhost:7023",
            "https://kohinoorea.com",
            "https://kohinoorea.com", 
            "http://dev-kohinoorea.kohinoorea.com",
            "https://dev-kohinoorea.kohinoorea.com", 
            "http://apikohinoorea.kohinoorea.com", 
            "http://dev-api-kohinoorea.kohinoorea.com")
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

app.UseMiddleware<GlobalExceptionMiddleware>();

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
