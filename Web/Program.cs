using System.Globalization;
using Web.Filters;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<TokenExpirationFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<TokenExpirationFilter>();
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthTokenHandler>();

var apiBaseUrl = builder.Configuration.GetValue<string>("Api:BaseUrl") ?? "http://localhost:5299/";
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
});

builder.Services.AddHttpClient<BusinessApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<StallApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<LanguageApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<StallNarrationContentApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();


// StallLocation and StallGeoFence clients
builder.Services.AddHttpClient<StallLocationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<StallMediaApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<SubscriptionApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<SubscriptionOrderApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<NarrationAudioApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<UserApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<QrCodeApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<StallGeoFenceApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<DeviceApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
}).AddHttpMessageHandler<AuthTokenHandler>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US")
    .AddSupportedUICultures("en-US"));
app.UseRouting();

app.UseSession(); // before UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
