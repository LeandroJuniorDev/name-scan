using NameScan.Platforms;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPlatformRegistry>(serviceProvider =>
    new PlatformRegistry(serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<NameScan.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
