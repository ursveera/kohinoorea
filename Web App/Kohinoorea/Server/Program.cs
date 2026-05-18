using Dapper;
using Kohinoorea.Client.Services;
using Kohinoorea.Server.Middleware;
using Kohinoorea.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy
            .WithOrigins("https://localhost:7023")
            .AllowAnyHeader()
            .AllowAnyMethod();
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

builder.Services.Configure<CurrencyPricingOptions>(
    builder.Configuration.GetSection("CurrencyPricing"));

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<CurrencyRateService>();
var app = builder.Build();

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

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("ClientCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapGet("/api/currency/rate/{currencyCode}",
    async (
        string currencyCode,
        CurrencyRateService currencyRateService) =>
    {
        var result = await currencyRateService.GetUsdRateAsync(currencyCode);
        return Results.Ok(result);
    });

app.Run();
