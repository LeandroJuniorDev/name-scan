# NameScan Aspire Adoption Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `.NET Aspire` to the existing NameScan solution with an AppHost and ServiceDefaults layer, preserve the current MVP behavior, and mark the current pre-Aspire state with the `v1.0.0-mvp` tag.

**Architecture:** Keep `NameScan` as the only product application and wrap it with Aspire operational concerns instead of splitting the domain into new services. Add `NameScan.AppHost` for orchestration and `NameScan.ServiceDefaults` for shared telemetry, health checks, and startup defaults, then migrate the web app to consume those defaults with minimal behavior change.

**Tech Stack:** .NET 10 SDK, ASP.NET Core, Blazor Web App, MudBlazor, xUnit, Aspire AppHost, Aspire ServiceDefaults, OpenTelemetry, health checks.

---

## File Structure

Expected files after implementation:

```text
/NameScan.sln
/README.md
/NameScan
  /Program.cs
  /NameScan.csproj
/NameScan.AppHost
  /AppHost.cs
  /NameScan.AppHost.csproj
  /appsettings.json
  /appsettings.Development.json
  /Properties
    /launchSettings.json
/NameScan.ServiceDefaults
  /Extensions.cs
  /NameScan.ServiceDefaults.csproj
/NameScan.Tests
  /Web
    /CheckStreamEndpointTests.cs
    /HealthEndpointsTests.cs
/docs/superpowers/specs
/docs/superpowers/plans
```

Responsibilities:

- `NameScan.AppHost`: local orchestration, dashboard experience, resource wiring for the existing `NameScan` web app.
- `NameScan.ServiceDefaults`: shared startup defaults for telemetry, service discovery, and health endpoints.
- `NameScan/Program.cs`: continues to own app startup, SSE endpoint mapping, and Blazor wiring, now with Aspire defaults composed in.
- `HealthEndpointsTests.cs`: protects the new `/health` and `/alive` development endpoints.
- `README.md`: explains how to run the app with and without Aspire and documents the MVP tag.

### Task 1: Mark The Current MVP Baseline

**Files:**
- Modify: git refs only
- Docs reference only: `README.md`

- [ ] **Step 1: Confirm the `main` baseline commit before tagging**

Run:

```bash
git rev-parse main
git log --oneline --decorate -n 3 main
```

Expected:

- `git rev-parse main` returns the current baseline SHA.
- At the time this plan was written, `main` pointed to `556033c`.
- The log shows `Merge pull request #13 from LeandroJuniorDev/feat/home-redesign` at the top.

- [ ] **Step 2: Create the annotated MVP tag**

Run:

```bash
git tag -a v1.0.0-mvp main -m "MVP baseline before .NET Aspire adoption"
git tag --list v1.0.0-mvp -n1
```

Expected:

- `git tag --list` prints `v1.0.0-mvp` with the annotation message.

- [ ] **Step 3: Optionally publish the tag to origin**

Run:

```bash
git push origin v1.0.0-mvp
```

Expected:

- Remote accepts the tag.
- GitHub can now use `v1.0.0-mvp` as a release anchor if desired.

- [ ] **Step 4: Commit nothing**

This task changes repository metadata, not tracked files. Do not create a commit here.

### Task 2: Install Aspire Templates And Scaffold The New Projects

**Files:**
- Create: `NameScan.AppHost/NameScan.AppHost.csproj`
- Create: `NameScan.AppHost/AppHost.cs`
- Create: `NameScan.AppHost/appsettings.json`
- Create: `NameScan.AppHost/appsettings.Development.json`
- Create: `NameScan.AppHost/Properties/launchSettings.json`
- Create: `NameScan.ServiceDefaults/NameScan.ServiceDefaults.csproj`
- Create: `NameScan.ServiceDefaults/Extensions.cs`
- Modify: `NameScan.sln`

- [ ] **Step 1: Check whether Aspire templates are already installed**

Run:

```bash
dotnet new list aspire
```

Expected:

- If templates are available, the command lists `aspire-apphost` and `aspire-servicedefaults`.
- If the command says `Nenhum modelo encontrado: 'aspire'`, continue to the next step.

- [ ] **Step 2: Install the Aspire project templates when missing**

Run:

```bash
dotnet new install Aspire.ProjectTemplates
dotnet new list aspire
```

Expected:

- Template installation succeeds.
- The follow-up list includes Aspire templates.

- [ ] **Step 3: Create the AppHost project from the official template**

Run:

```bash
dotnet new aspire-apphost -o NameScan.AppHost
dotnet sln NameScan.sln add NameScan.AppHost/NameScan.AppHost.csproj
```

Expected:

- The `NameScan.AppHost` directory exists.
- The AppHost project is part of `NameScan.sln`.

- [ ] **Step 4: Create the ServiceDefaults project from the official template**

Run:

```bash
dotnet new aspire-servicedefaults -o NameScan.ServiceDefaults
dotnet sln NameScan.sln add NameScan.ServiceDefaults/NameScan.ServiceDefaults.csproj
```

Expected:

- The `NameScan.ServiceDefaults` directory exists.
- The ServiceDefaults project is part of `NameScan.sln`.

- [ ] **Step 5: Add project references to wire the new structure**

Run:

```bash
dotnet add NameScan.AppHost/NameScan.AppHost.csproj reference NameScan/NameScan.csproj
dotnet add NameScan/NameScan.csproj reference NameScan.ServiceDefaults/NameScan.ServiceDefaults.csproj
```

Expected:

- `NameScan.AppHost` can orchestrate the existing web app.
- `NameScan` consumes the shared defaults project.

- [ ] **Step 6: Build the solution to verify the scaffold**

Run:

```bash
dotnet build NameScan.sln
```

Expected:

- Build succeeds with the newly added projects.

- [ ] **Step 7: Commit the Aspire scaffolding**

Run:

```bash
git add NameScan.sln NameScan.AppHost NameScan.ServiceDefaults NameScan/NameScan.csproj
git commit -m "build: scaffold Aspire app host"
```

Expected:

- A commit records the new orchestration and shared defaults skeleton.

### Task 3: Integrate ServiceDefaults Into The Existing Web App

**Files:**
- Modify: `NameScan/Program.cs`
- Modify: `NameScan/NameScan.csproj`
- Modify: `NameScan.ServiceDefaults/Extensions.cs`
- Test: `NameScan.Tests/Web/CheckStreamEndpointTests.cs`
- Create: `NameScan.Tests/Web/HealthEndpointsTests.cs`

- [ ] **Step 1: Write the failing health endpoint tests**

Create `NameScan.Tests/Web/HealthEndpointsTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

namespace NameScan.Tests.Web;

public sealed class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task DevelopmentHealthEndpoints_ReturnHealthy(string path)
    {
        var client = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();

        using var response = await client.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Healthy", content);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task ProductionHealthEndpoints_AreNotExposedByDefault(string path)
    {
        var client = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run:

```bash
dotnet test NameScan.sln --filter HealthEndpointsTests
```

Expected:

- Tests fail because `/health` and `/alive` are not mapped yet.

- [ ] **Step 3: Update `Program.cs` to use the Aspire defaults**

Replace the startup shape in `NameScan/Program.cs` with:

```csharp
using System.Text.Json;
using NameScan.Features.Checks;
using NameScan.Features.Reporting;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<NicknameValidator>();
builder.Services.AddSingleton<SuggestionService>();
builder.Services.AddSingleton<ReportFormatter>();
builder.Services.AddSingleton<IPlatformRegistry>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new PlatformRegistry(httpClientFactory.CreateClient());
});
builder.Services.AddScoped<HandleCheckService>();

var app = builder.Build();
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/api/check/stream", async (
    string? nickname,
    HandleCheckService checkService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.Headers.CacheControl = "no-cache";
    if (string.Equals(httpContext.Request.Protocol, "HTTP/1.1", StringComparison.OrdinalIgnoreCase))
    {
        httpContext.Response.Headers.Connection = "keep-alive";
    }

    httpContext.Response.ContentType = "text/event-stream; charset=utf-8";

    await foreach (var streamEvent in checkService.StreamAsync(nickname, cancellationToken))
    {
        var eventName = streamEvent.Kind switch
        {
            CheckStreamEventKind.Result => "result",
            CheckStreamEventKind.Done => "done",
            CheckStreamEventKind.Error => "error",
            _ => "error"
        };

        var json = JsonSerializer.Serialize(streamEvent, jsonOptions);
        await httpContext.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
});

app.MapRazorComponents<NameScan.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
```

- [ ] **Step 4: Keep the ServiceDefaults extension focused and review the generated `Extensions.cs`**

Ensure `NameScan.ServiceDefaults/Extensions.cs` keeps the generated Aspire shape, including these two surface APIs:

```csharp
public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });

    return builder;
}

public static WebApplication MapDefaultEndpoints(this WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = static r => r.Tags.Contains("live")
        });
    }

    return app;
}
```

Keep any additional generated telemetry helpers from the template unless there is a concrete NameScan-specific reason to simplify them.

- [ ] **Step 5: Run the targeted web tests**

Run:

```bash
dotnet test NameScan.sln --filter "HealthEndpointsTests|CheckStreamEndpointTests"
```

Expected:

- Existing SSE tests still pass.
- New health endpoint tests pass.

- [ ] **Step 6: Commit the startup integration**

Run:

```bash
git add NameScan/Program.cs NameScan/NameScan.csproj NameScan.ServiceDefaults/Extensions.cs NameScan.Tests/Web/HealthEndpointsTests.cs NameScan.Tests/Web/CheckStreamEndpointTests.cs
git commit -m "feat: adopt Aspire service defaults"
```

Expected:

- A commit records the web app integration with minimal functional change.

### Task 4: Add AppHost Orchestration For The Existing NameScan App

**Files:**
- Modify: `NameScan.AppHost/AppHost.cs`
- Modify: `NameScan.AppHost/NameScan.AppHost.csproj`
- Modify: `NameScan.AppHost/appsettings.json`
- Modify: `NameScan.AppHost/appsettings.Development.json`

- [ ] **Step 1: Write the AppHost resource registration**

Update `NameScan.AppHost/AppHost.cs` to orchestrate the existing web app:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.NameScan>("namescan")
    .WithExternalHttpEndpoints()
    .WithHttpsHealthCheck("/health");

builder.Build().Run();
```

- [ ] **Step 2: Ensure the AppHost project references the web app project**

Confirm `NameScan.AppHost/NameScan.AppHost.csproj` contains the Aspire SDK and the `NameScan` project reference:

```xml
<Project Sdk="Aspire.AppHost.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\NameScan\NameScan.csproj" />
  </ItemGroup>
</Project>
```

Keep any additional template-generated properties that are needed by the installed Aspire SDK.

- [ ] **Step 3: Run the AppHost to verify the orchestration path**

Run:

```bash
dotnet run --project NameScan.AppHost
```

Expected:

- Aspire starts the AppHost successfully.
- The dashboard opens or prints local URLs.
- The `namescan` resource starts without changing the user-facing behavior of the app.

- [ ] **Step 4: Smoke-test the web app through the AppHost**

While the AppHost is running, use the dashboard or startup console output to open the `namescan` resource URL and verify the root page loads.

Expected:

- The `namescan` resource appears as running and healthy in the Aspire dashboard.
- Opening the resource URL loads the existing home page without changing the user-facing behavior.

- [ ] **Step 5: Build the solution again**

Run:

```bash
dotnet build NameScan.sln
```

Expected:

- The solution still builds cleanly with the AppHost wiring in place.

- [ ] **Step 6: Commit the AppHost orchestration**

Run:

```bash
git add NameScan.AppHost
git commit -m "feat: orchestrate NameScan with Aspire app host"
```

Expected:

- A commit records the orchestrated local runtime.

### Task 5: Add Minimal NameScan-Specific Telemetry And Operational Documentation

**Files:**
- Modify: `NameScan/Program.cs`
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-05-02-aspire-adoption-design.md` only if the implementation intentionally diverges

- [ ] **Step 1: Add scoped logging around stream lifecycle only where it already fits**

Keep instrumentation minimal and reuse existing logger calls. The target is to ensure these messages exist and remain structured:

```csharp
Logger.LogInformation("NameScan search started");
Logger.LogInformation("NameScan report copied");
Logger.LogInformation("NameScan result link clicked {Platform}", result.Platform);
Logger.LogInformation("NameScan support link clicked");
```

Add one server-side log in the SSE endpoint if a request begins, for example:

```csharp
app.MapGet("/api/check/stream", async (
    string? nickname,
    HandleCheckService checkService,
    HttpContext httpContext,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("NameScan stream requested for nickname {Nickname}", nickname);

    // existing SSE body...
});
```

- [ ] **Step 2: Update the README for the Aspire workflow**

Extend `README.md` with:

````md
## Execução

Fluxo recomendado com Aspire:

```bash
dotnet run --project NameScan.AppHost
```

Fluxo direto do app web:

```bash
dotnet run --project NameScan
```

## Marco Do MVP

O estado funcional anterior à adoção do `.NET Aspire` foi marcado com a tag anotada `v1.0.0-mvp`.
````

Preserve the existing Portuguese tone and keep the documentation short.

- [ ] **Step 3: Run the smallest meaningful verification set**

Run:

```bash
dotnet test NameScan.sln --filter "CheckStreamEndpointTests|HealthEndpointsTests|HomePageTests"
dotnet build NameScan.sln
```

Expected:

- Web startup and homepage tests pass.
- The solution builds after all docs and startup changes.

- [ ] **Step 4: Commit the docs and telemetry polish**

Run:

```bash
git add NameScan/Program.cs README.md
git commit -m "docs: document Aspire workflow"
```

Expected:

- A final implementation commit records the operational guidance.

## Self-Review

Spec coverage check:

- MVP tag strategy is covered in Task 1.
- AppHost introduction is covered in Task 2 and Task 4.
- ServiceDefaults adoption is covered in Task 2 and Task 3.
- Health checks, telemetry, and development observability are covered in Task 3 and Task 5.
- Deploy-ready local orchestration and documentation are covered in Task 4 and Task 5.
- Preservation of the existing SSE contract is protected by re-running `CheckStreamEndpointTests` in Task 3 and Task 5.

Placeholder scan:

- No `TODO`, `TBD`, or deferred “fill this in later” placeholders remain.
- Commands, files, and test targets are concrete.

Type consistency:

- `AddServiceDefaults` and `MapDefaultEndpoints` are used consistently between `NameScan` and `NameScan.ServiceDefaults`.
- `Projects.NameScan` matches the expected generated project metadata name from the AppHost project reference.
- Health endpoint names stay consistent as `/health` and `/alive`.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-02-aspire-adoption-implementation.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
