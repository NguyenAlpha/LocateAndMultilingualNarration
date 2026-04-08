using Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthTokenHandler>();

var apiBaseUrl = builder.Configuration.GetValue<string>("Api:BaseUrl") ?? "https://localhost:7188/";
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

builder.Services.AddHttpClient<NarrationAudioApiClient>(client =>
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

//builder.Services.AddHttpClient<StallGeoFenceApiClient>(client =>
//{
//    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
//}).AddHttpMessageHandler<AuthTokenHandler>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession(); // before UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
