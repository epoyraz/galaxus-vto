using System.Net.Http.Headers;
using BflVirtualTryOn.Configuration;
using BflVirtualTryOn.Endpoints;
using BflVirtualTryOn.Services;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Options: bind "Bfl" section, fall back to BFL_API_KEY env var for the key ---
builder.Services
    .AddOptions<BflOptions>()
    .Bind(builder.Configuration.GetSection(BflOptions.SectionName))
    .PostConfigure(options =>
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            options.ApiKey = builder.Configuration["BFL_API_KEY"] ?? string.Empty;
        }
    });

// --- Typed HttpClient for the BFL API (x-key + Accept headers applied to every call) ---
builder.Services.AddHttpClient<IBflVtoClient, BflVtoClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<BflOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Add("x-key", options.ApiKey);
    }
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BflExceptionHandler>();
builder.Services.AddOpenApi();

// Public API: no authentication, callable from any origin.
const string PublicCorsPolicy = "public";
builder.Services.AddCors(options => options.AddPolicy(PublicCorsPolicy, policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(PublicCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("FLUX Virtual Try-On API"));
}

// Serve the bundled demo fitting-room page from wwwroot at "/".
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapVtoEndpoints();

app.MapGet("/health", (IOptions<BflOptions> options) => Results.Ok(new
{
    status = "ok",
    apiKeyConfigured = !string.IsNullOrWhiteSpace(options.Value.ApiKey),
})).WithTags("Health");

// Friendly startup warning if the API key is missing.
var startupOptions = app.Services.GetRequiredService<IOptions<BflOptions>>().Value;
if (string.IsNullOrWhiteSpace(startupOptions.ApiKey))
{
    app.Logger.LogWarning(
        "No BFL API key configured. Set it via 'dotnet user-secrets set \"Bfl:ApiKey\" <key>' " +
        "or the BFL_API_KEY environment variable.");
}

app.Run();
