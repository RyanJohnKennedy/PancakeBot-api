using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PancakeBot.Api.Middleware;
using PancakeBot.Api.Option;
using Microsoft.OpenApi.Models;
using PancakeBot.Api.Client;
using PancakeBot.Api.data;
using PancakeBot.Api.Handler;
using PancakeBot.Api.provider;
using PancakeBot.Api.Service;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services
    .AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "ApiKey is missing")
    .ValidateOnStart();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API Key",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        }
    };

    c.AddSecurityDefinition("ApiKey", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services
    .AddOptions<TrackmaniaOptions>()
    .Bind(builder.Configuration.GetSection(TrackmaniaOptions.SectionName))
    .Validate(o => Uri.TryCreate(o.CoreUrl, UriKind.Absolute, out _), "Trackmania:CoreUrl must be an absolute URL")
    .Validate(o => Uri.TryCreate(o.LiveUrl, UriKind.Absolute, out _), "Trackmania:LiveUrl must be an absolute URL")
    .Validate(o => Uri.TryCreate(o.OAuthUrl, UriKind.Absolute, out _), "Trackmania:OAuthUrl must be an absolute URL")
    .Validate(o => !string.IsNullOrWhiteSpace(o.UserAgent), "Trackmania:UserAgent is required")
    .Validate(o => o.DailyLeaderboardSize > 0, "Trackmania:DailyLeaderboardSize must be greater than zero")
    .Validate(o => o.Regions.TryGetValue(o.DefaultRegion, out var zoneId) && !string.IsNullOrWhiteSpace(zoneId),
        "Trackmania:DefaultRegion must reference a configured region")
    .ValidateOnStart();

builder.Services.AddHttpClient("TrackmaniaAuth", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Trackmania:CoreUrl"]!);
});
builder.Services.AddSingleton<ITrackmaniaAuthService, TrackmaniaAuthService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TrackmaniaLiveService>();
builder.Services.AddScoped<IPreviousTotdService, PreviousTotdService>();
builder.Services.AddScoped<ITotdSyncService, TotdSyncService>();
builder.Services.AddScoped<ITotdLeaderboardService, TotdLeaderboardService>();
builder.Services.AddScoped<ITotdMonthLeaderboardService, TotdMonthLeaderboardService>();

builder.Services.AddSingleton<TrackmaniaOAuthTokenProvider>();

builder.Services.AddHttpClient<TrackmaniaOAuthService>((c) =>
{
    c.BaseAddress = new Uri(
        builder.Configuration["Trackmania:OAuthUrl"]!);
});

// CORE API CLIENT
builder.Services.AddHttpClient<ITrackmaniaCoreClient, TrackmaniaCoreClient>(c =>
{
    c.BaseAddress = new Uri(
        builder.Configuration["Trackmania:CoreUrl"]!);
})
.AddHttpMessageHandler(sp =>
    new TrackmaniaAuthHandler(
        sp.GetRequiredService<ITrackmaniaAuthService>(),
        sp.GetRequiredService<IOptions<TrackmaniaOptions>>(),
        "NadeoServices"
    )
);

// LIVE API CLIENT
builder.Services.AddHttpClient<ITrackmaniaLiveClient, TrackmaniaLiveClient>(c =>
{
    c.BaseAddress = new Uri(
        builder.Configuration["Trackmania:LiveUrl"]!);
})
.AddHttpMessageHandler(sp =>
    new TrackmaniaAuthHandler(
        sp.GetRequiredService<ITrackmaniaAuthService>(),
        sp.GetRequiredService<IOptions<TrackmaniaOptions>>(),
        "NadeoLiveServices"
    )
);

// OAuth API CLIENT
builder.Services.AddHttpClient<ITrackmaniaOAuthClient, TrackmaniaOAuthClient>((c) =>
{
    c.BaseAddress = new Uri(
        builder.Configuration["Trackmania:OAuthUrl"]!);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
