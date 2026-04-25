# NameScan MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the NameScan MVP: a Brazil-first Blazor/MudBlazor app that streams nickname availability checks through SSE across 8 required targets.

**Architecture:** The app is a single ASP.NET Core Blazor Web App with Minimal API endpoints for streaming checks. Domain logic lives in focused feature folders: validation, suggestions, checks, platforms, caching, and UI state. Platform checkers are isolated behind `IPlatformChecker`, making HTTP/domain behavior testable without external network calls in unit tests.

**Tech Stack:** .NET 10 SDK, ASP.NET Core, Blazor Web App, MudBlazor, Server-Sent Events, `HttpClientFactory`, `IMemoryCache`, xUnit, bUnit.

---

## File Structure

Create this structure from the repository root:

```text
/NameScan.sln
/NameScan
  /Components
    /Layout
      MainLayout.razor
    /Pages
      Home.razor
    /_Imports.razor
    /App.razor
    /Routes.razor
  /Features
    /Checks
      CheckSummary.cs
      CheckStatus.cs
      CheckStreamEvent.cs
      ConfidenceLevel.cs
      HandleCheckService.cs
      PlatformCheckResult.cs
    /Reporting
      ReportFormatter.cs
    /Suggestions
      SuggestionService.cs
  /Platforms
    BaseHttpPlatformChecker.cs
    DotComBrDomainChecker.cs
    DotComDomainChecker.cs
    DomainLookupResult.cs
    DomainPlatformChecker.cs
    GitHubChecker.cs
    IPlatformChecker.cs
    IPlatformRegistry.cs
    PlatformRegistry.cs
    SocialPlatformChecker.cs
  /Validation
    NicknameValidationResult.cs
    NicknameValidator.cs
  /wwwroot
    /css
      app.css
    /js
      check-stream.js
  Program.cs
  NameScan.csproj
/NameScan.Tests
  /Features
    /Checks
      HandleCheckServiceTests.cs
    /Reporting
      ReportFormatterTests.cs
    /Suggestions
      SuggestionServiceTests.cs
  /Platforms
    PlatformCheckerTests.cs
  /Validation
    NicknameValidatorTests.cs
  /Web
    CheckStreamEndpointTests.cs
    HomePageTests.cs
  Fakes.cs
  NameScan.Tests.csproj
```

Responsibilities:

- `NicknameValidator`: general input rules shared by API and UI.
- `SuggestionService`: deterministic, rule-based alternatives.
- `ReportFormatter`: plain-text report generation.
- `IPlatformChecker`: contract for platform-specific checks.
- `SocialPlatformChecker`: reusable HTTP checker for predictable profile URLs.
- `DomainPlatformChecker`: reusable checker for `.com` and `.com.br`.
- `PlatformRegistry`: ordered list of the 8 required targets.
- `HandleCheckService`: validation, cache, parallel execution, timeout isolation, summary generation.
- `check-stream.js`: browser `EventSource` bridge for Blazor.
- `Home.razor`: MudBlazor UI and user-facing states.

---

### Task 1: Scaffold Solution And Test Projects

**Files:**
- Create: `NameScan.sln`
- Create: `NameScan/NameScan.csproj`
- Create: `NameScan.Tests/NameScan.Tests.csproj`
- Modify: `NameScan/Program.cs`
- Modify: `NameScan/Components/_Imports.razor`

- [ ] **Step 1: Create the solution and Blazor app**

Run:

```bash
dotnet new sln -n NameScan
dotnet new blazor -n NameScan -o NameScan
dotnet sln NameScan.sln add NameScan/NameScan.csproj
```

Expected: `NameScan.sln` exists and includes `NameScan/NameScan.csproj`.

- [ ] **Step 2: Create the test project**

Run:

```bash
dotnet new xunit -n NameScan.Tests -o NameScan.Tests
dotnet sln NameScan.sln add NameScan.Tests/NameScan.Tests.csproj
dotnet add NameScan.Tests/NameScan.Tests.csproj reference NameScan/NameScan.csproj
```

Expected: `NameScan.Tests` is part of the solution and references `NameScan`.

- [ ] **Step 3: Add UI and test packages**

Run:

```bash
dotnet add NameScan/NameScan.csproj package MudBlazor
dotnet add NameScan.Tests/NameScan.Tests.csproj package bunit
dotnet add NameScan.Tests/NameScan.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

Expected: package references are added to the relevant `.csproj` files.

- [ ] **Step 4: Configure MudBlazor base app services**

Replace `NameScan/Program.cs` with:

```csharp
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

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
```

- [ ] **Step 5: Import MudBlazor namespaces**

Append these lines to `NameScan/Components/_Imports.razor` if they are not already present:

```razor
@using MudBlazor
```

- [ ] **Step 6: Verify scaffold builds**

Run:

```bash
dotnet build NameScan.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit scaffold**

Run:

```bash
git add NameScan.sln NameScan NameScan.Tests
git commit -m "build: scaffold NameScan Blazor solution"
```

Expected: commit succeeds.

---

### Task 2: Add Validation Domain

**Files:**
- Create: `NameScan/Validation/NicknameValidationResult.cs`
- Create: `NameScan/Validation/NicknameValidator.cs`
- Create: `NameScan.Tests/Validation/NicknameValidatorTests.cs`

- [ ] **Step 1: Write failing validator tests**

Create `NameScan.Tests/Validation/NicknameValidatorTests.cs`:

```csharp
using NameScan.Validation;

namespace NameScan.Tests.Validation;

public sealed class NicknameValidatorTests
{
    private readonly NicknameValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsEmptyInput(string input)
    {
        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Digite um nickname para verificar.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcde")]
    public void Validate_RejectsInvalidLength(string input)
    {
        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Use entre 2 e 30 caracteres.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("minha marca")]
    [InlineData("marca!")]
    [InlineData("ação")]
    public void Validate_RejectsUnsupportedCharacters(string input)
    {
        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Use apenas letras sem acento, números, ponto, underline ou hífen.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(" minhamarca ", "minhamarca")]
    [InlineData("minha.marca", "minha.marca")]
    [InlineData("minha_marca", "minha_marca")]
    [InlineData("minha-marca", "minha-marca")]
    [InlineData("Marca123", "marca123")]
    public void Validate_ReturnsNormalizedNickname(string input, string expected)
    {
        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.NormalizedNickname);
        Assert.Null(result.ErrorMessage);
    }
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run:

```bash
dotnet test NameScan.sln --filter NicknameValidatorTests
```

Expected: tests fail because `NicknameValidator` does not exist.

- [ ] **Step 3: Add validation result**

Create `NameScan/Validation/NicknameValidationResult.cs`:

```csharp
namespace NameScan.Validation;

public sealed record NicknameValidationResult(
    bool IsValid,
    string NormalizedNickname,
    string? ErrorMessage)
{
    public static NicknameValidationResult Valid(string normalizedNickname) =>
        new(true, normalizedNickname, null);

    public static NicknameValidationResult Invalid(string errorMessage) =>
        new(false, string.Empty, errorMessage);
}
```

- [ ] **Step 4: Add validator implementation**

Create `NameScan/Validation/NicknameValidator.cs`:

```csharp
using System.Text.RegularExpressions;

namespace NameScan.Validation;

public sealed partial class NicknameValidator
{
    public NicknameValidationResult Validate(string? input)
    {
        var normalized = (input ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return NicknameValidationResult.Invalid("Digite um nickname para verificar.");
        }

        if (normalized.Length < 2 || normalized.Length > 30)
        {
            return NicknameValidationResult.Invalid("Use entre 2 e 30 caracteres.");
        }

        if (!AllowedCharacters().IsMatch(normalized))
        {
            return NicknameValidationResult.Invalid("Use apenas letras sem acento, números, ponto, underline ou hífen.");
        }

        return NicknameValidationResult.Valid(normalized);
    }

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex AllowedCharacters();
}
```

- [ ] **Step 5: Run validator tests**

Run:

```bash
dotnet test NameScan.sln --filter NicknameValidatorTests
```

Expected: all `NicknameValidatorTests` pass.

- [ ] **Step 6: Commit validation**

Run:

```bash
git add NameScan/Validation NameScan.Tests/Validation
git commit -m "feat: add nickname validation"
```

Expected: commit succeeds.

---

### Task 3: Add Suggestions And Report Formatting

**Files:**
- Create: `NameScan/Features/Suggestions/SuggestionService.cs`
- Create: `NameScan/Features/Reporting/ReportFormatter.cs`
- Create: `NameScan.Tests/Features/Suggestions/SuggestionServiceTests.cs`
- Create: `NameScan.Tests/Features/Reporting/ReportFormatterTests.cs`
- Create: `NameScan/Features/Checks/CheckStatus.cs`
- Create: `NameScan/Features/Checks/ConfidenceLevel.cs`
- Create: `NameScan/Features/Checks/PlatformCheckResult.cs`

- [ ] **Step 1: Write failing suggestion tests**

Create `NameScan.Tests/Features/Suggestions/SuggestionServiceTests.cs`:

```csharp
using NameScan.Features.Suggestions;

namespace NameScan.Tests.Features.Suggestions;

public sealed class SuggestionServiceTests
{
    [Fact]
    public void Generate_ReturnsDeterministicBrazilFirstSuggestions()
    {
        var service = new SuggestionService();

        var suggestions = service.Generate("minhamarca");

        Assert.Equal(
        [
            "minhamarcaapp",
            "useminhamarca",
            "minhamarcaoficial",
            "minhamarca_io",
            "getminhamarca",
            "minhamarcaHQ",
            "minhamarcaBrasil"
        ], suggestions);
    }

    [Fact]
    public void Generate_DoesNotReturnSuggestionsLongerThanThirtyCharacters()
    {
        var service = new SuggestionService();

        var suggestions = service.Generate("nomemuitolongoquasecomtrinta");

        Assert.All(suggestions, suggestion => Assert.InRange(suggestion.Length, 2, 30));
    }
}
```

- [ ] **Step 2: Write failing report tests**

Create `NameScan.Tests/Features/Reporting/ReportFormatterTests.cs`:

```csharp
using NameScan.Features.Checks;
using NameScan.Features.Reporting;

namespace NameScan.Tests.Features.Reporting;

public sealed class ReportFormatterTests
{
    [Fact]
    public void Format_BuildsPlainTextReportInPortuguese()
    {
        var formatter = new ReportFormatter();
        var results = new[]
        {
            new PlatformCheckResult("Instagram", "https://instagram.com/minhamarca", CheckStatus.Occupied, ConfidenceLevel.High, "Perfil encontrado"),
            new PlatformCheckResult("GitHub", "https://github.com/minhamarca", CheckStatus.Available, ConfidenceLevel.High, "Perfil não encontrado"),
            new PlatformCheckResult(".com.br", "https://registro.br/tecnologia/ferramentas/whois?search=minhamarca.com.br", CheckStatus.Inconclusive, ConfidenceLevel.Low, "Consulta inconclusiva")
        };

        var report = formatter.Format("minhamarca", results);

        var expected = string.Join(Environment.NewLine,
        [
            "Resultado para: minhamarca",
            string.Empty,
            "Instagram: Ocupado",
            "GitHub: Disponível",
            ".com.br: Inconclusivo"
        ]);

        Assert.Equal(expected, report);
    }
}
```

- [ ] **Step 3: Run tests and confirm failure**

Run:

```bash
dotnet test NameScan.sln --filter "SuggestionServiceTests|ReportFormatterTests"
```

Expected: tests fail because services and check models do not exist.

- [ ] **Step 4: Add check models**

Create `NameScan/Features/Checks/CheckStatus.cs`:

```csharp
namespace NameScan.Features.Checks;

public enum CheckStatus
{
    Available,
    Occupied,
    Invalid,
    Inconclusive,
    Error
}
```

Create `NameScan/Features/Checks/ConfidenceLevel.cs`:

```csharp
namespace NameScan.Features.Checks;

public enum ConfidenceLevel
{
    Low,
    Medium,
    High
}
```

Create `NameScan/Features/Checks/PlatformCheckResult.cs`:

```csharp
namespace NameScan.Features.Checks;

public sealed record PlatformCheckResult(
    string Platform,
    string Url,
    CheckStatus Status,
    ConfidenceLevel Confidence,
    string? Note);
```

- [ ] **Step 5: Add suggestion service**

Create `NameScan/Features/Suggestions/SuggestionService.cs`:

```csharp
namespace NameScan.Features.Suggestions;

public sealed class SuggestionService
{
    public IReadOnlyList<string> Generate(string nickname)
    {
        var candidates = new[]
        {
            $"{nickname}app",
            $"use{nickname}",
            $"{nickname}oficial",
            $"{nickname}_io",
            $"get{nickname}",
            $"{nickname}HQ",
            $"{nickname}Brasil"
        };

        return candidates
            .Where(candidate => candidate.Length is >= 2 and <= 30)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
```

- [ ] **Step 6: Add report formatter**

Create `NameScan/Features/Reporting/ReportFormatter.cs`:

```csharp
using NameScan.Features.Checks;

namespace NameScan.Features.Reporting;

public sealed class ReportFormatter
{
    public string Format(string query, IEnumerable<PlatformCheckResult> results)
    {
        var lines = new List<string>
        {
            $"Resultado para: {query}",
            string.Empty
        };

        lines.AddRange(results.Select(result => $"{result.Platform}: {ToPortuguese(result.Status)}"));

        return string.Join(Environment.NewLine, lines);
    }

    public static string ToPortuguese(CheckStatus status) =>
        status switch
        {
            CheckStatus.Available => "Disponível",
            CheckStatus.Occupied => "Ocupado",
            CheckStatus.Invalid => "Inválido",
            CheckStatus.Inconclusive => "Inconclusivo",
            CheckStatus.Error => "Erro",
            _ => "Inconclusivo"
        };

    public static string ToPortuguese(ConfidenceLevel confidence) =>
        confidence switch
        {
            ConfidenceLevel.Low => "Baixa",
            ConfidenceLevel.Medium => "Média",
            ConfidenceLevel.High => "Alta",
            _ => "Baixa"
        };
}
```

- [ ] **Step 7: Run tests**

Run:

```bash
dotnet test NameScan.sln --filter "SuggestionServiceTests|ReportFormatterTests"
```

Expected: all selected tests pass.

- [ ] **Step 8: Commit suggestions and report formatting**

Run:

```bash
git add NameScan/Features NameScan.Tests/Features
git commit -m "feat: add suggestions and report formatting"
```

Expected: commit succeeds.

---

### Task 4: Add Platform Checker Abstractions And Required Targets

**Files:**
- Create: `NameScan/Platforms/IPlatformChecker.cs`
- Create: `NameScan/Platforms/BaseHttpPlatformChecker.cs`
- Create: `NameScan/Platforms/SocialPlatformChecker.cs`
- Create: `NameScan/Platforms/DomainLookupResult.cs`
- Create: `NameScan/Platforms/DomainPlatformChecker.cs`
- Create: `NameScan/Platforms/DotComDomainChecker.cs`
- Create: `NameScan/Platforms/DotComBrDomainChecker.cs`
- Create: `NameScan/Platforms/GitHubChecker.cs`
- Create: `NameScan/Platforms/IPlatformRegistry.cs`
- Create: `NameScan/Platforms/PlatformRegistry.cs`
- Create: `NameScan.Tests/Platforms/PlatformCheckerTests.cs`
- Create: `NameScan.Tests/Fakes.cs`
- Modify: `NameScan/Program.cs`

- [ ] **Step 1: Write failing platform tests**

Create `NameScan.Tests/Fakes.cs`:

```csharp
using System.Net;

namespace NameScan.Tests;

public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responder(request));
}

public static class TestHttpClientFactory
{
    public static HttpClient Create(HttpStatusCode statusCode, string body = "") =>
        new(new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        }));
}
```

Create `NameScan.Tests/Platforms/PlatformCheckerTests.cs`:

```csharp
using System.Net;
using NameScan.Features.Checks;
using NameScan.Platforms;

namespace NameScan.Tests.Platforms;

public sealed class PlatformCheckerTests
{
    [Fact]
    public async Task SocialChecker_ReturnsOccupiedForSuccess()
    {
        var checker = new SocialPlatformChecker(
            "instagram",
            "Instagram",
            nickname => new Uri($"https://instagram.com/{nickname}"),
            TestHttpClientFactory.Create(HttpStatusCode.OK));

        var result = await checker.CheckAsync("minhamarca", CancellationToken.None);

        Assert.Equal(CheckStatus.Occupied, result.Status);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
        Assert.Equal("https://instagram.com/minhamarca", result.Url);
    }

    [Fact]
    public async Task SocialChecker_ReturnsAvailableForNotFound()
    {
        var checker = new SocialPlatformChecker(
            "github",
            "GitHub",
            nickname => new Uri($"https://github.com/{nickname}"),
            TestHttpClientFactory.Create(HttpStatusCode.NotFound));

        var result = await checker.CheckAsync("minhamarca", CancellationToken.None);

        Assert.Equal(CheckStatus.Available, result.Status);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    [Fact]
    public async Task SocialChecker_ReturnsInconclusiveForAmbiguousRedirect()
    {
        var checker = new SocialPlatformChecker(
            "x",
            "X",
            nickname => new Uri($"https://x.com/{nickname}"),
            TestHttpClientFactory.Create(HttpStatusCode.Redirect));

        var result = await checker.CheckAsync("minhamarca", CancellationToken.None);

        Assert.Equal(CheckStatus.Inconclusive, result.Status);
        Assert.Equal(ConfidenceLevel.Low, result.Confidence);
    }

    [Fact]
    public async Task GitHubChecker_ReturnsInvalidForDot()
    {
        var checker = new GitHubChecker(TestHttpClientFactory.Create(HttpStatusCode.OK));

        var result = await checker.CheckAsync("minha.marca", CancellationToken.None);

        Assert.Equal(CheckStatus.Invalid, result.Status);
    }

    [Fact]
    public void PlatformRegistry_ContainsRequiredEightTargetsInOrder()
    {
        var registry = new PlatformRegistry(new HttpClient());

        Assert.Equal(
        [
            "Instagram",
            "TikTok",
            "X",
            "YouTube",
            "GitHub",
            "Twitch",
            ".com",
            ".com.br"
        ], registry.GetAll().Select(checker => checker.Name));
    }
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run:

```bash
dotnet test NameScan.sln --filter PlatformCheckerTests
```

Expected: tests fail because platform classes do not exist.

- [ ] **Step 3: Add platform contracts**

Create `NameScan/Platforms/IPlatformChecker.cs`:

```csharp
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public interface IPlatformChecker
{
    string Id { get; }
    string Name { get; }
    Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken);
}
```

Create `NameScan/Platforms/IPlatformRegistry.cs`:

```csharp
namespace NameScan.Platforms;

public interface IPlatformRegistry
{
    IReadOnlyList<IPlatformChecker> GetAll();
}
```

- [ ] **Step 4: Add reusable HTTP social checker**

Create `NameScan/Platforms/BaseHttpPlatformChecker.cs`:

```csharp
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public abstract class BaseHttpPlatformChecker(HttpClient httpClient) : IPlatformChecker
{
    protected HttpClient HttpClient { get; } = httpClient;

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken);

    protected static PlatformCheckResult Invalid(string platform, string url, string note) =>
        new(platform, url, CheckStatus.Invalid, ConfidenceLevel.High, note);
}
```

Create `NameScan/Platforms/SocialPlatformChecker.cs`:

```csharp
using System.Net;
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public sealed class SocialPlatformChecker(
    string id,
    string name,
    Func<string, Uri> buildUri,
    HttpClient httpClient) : BaseHttpPlatformChecker(httpClient)
{
    public override string Id { get; } = id;
    public override string Name { get; } = name;

    public override async Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken)
    {
        var uri = buildUri(nickname);

        try
        {
            using var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => new(Name, uri.ToString(), CheckStatus.Occupied, ConfidenceLevel.High, "Perfil encontrado."),
                HttpStatusCode.NotFound => new(Name, uri.ToString(), CheckStatus.Available, ConfidenceLevel.High, "Perfil não encontrado."),
                HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests => new(Name, uri.ToString(), CheckStatus.Inconclusive, ConfidenceLevel.Low, "A plataforma bloqueou ou limitou a verificação."),
                HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect => new(Name, uri.ToString(), CheckStatus.Inconclusive, ConfidenceLevel.Low, "Redirect ambíguo."),
                _ => new(Name, uri.ToString(), CheckStatus.Inconclusive, ConfidenceLevel.Low, $"Resposta HTTP {(int)response.StatusCode}.")
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(Name, uri.ToString(), CheckStatus.Error, ConfidenceLevel.Low, "Falha técnica temporária.");
        }
    }
}
```

- [ ] **Step 5: Add GitHub-specific validation**

Create `NameScan/Platforms/GitHubChecker.cs`:

```csharp
using System.Text.RegularExpressions;
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public sealed partial class GitHubChecker(HttpClient httpClient)
    : BaseHttpPlatformChecker(httpClient)
{
    public override string Id => "github";
    public override string Name => "GitHub";

    public override async Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken)
    {
        var uri = new Uri($"https://github.com/{nickname}");

        if (!GitHubName().IsMatch(nickname) || nickname.StartsWith('-') || nickname.EndsWith('-'))
        {
            return Invalid(Name, uri.ToString(), "GitHub permite letras, números e hífen, sem hífen no início ou fim.");
        }

        var checker = new SocialPlatformChecker(Id, Name, value => new Uri($"https://github.com/{value}"), HttpClient);
        return await checker.CheckAsync(nickname, cancellationToken);
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex GitHubName();
}
```

- [ ] **Step 6: Add domain checker classes**

Create `NameScan/Platforms/DomainLookupResult.cs`:

```csharp
namespace NameScan.Platforms;

public sealed record DomainLookupResult(bool Exists, bool IsConclusive, string Note);
```

Create `NameScan/Platforms/DomainPlatformChecker.cs`:

```csharp
using System.Net;
using System.Net.Sockets;
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public abstract class DomainPlatformChecker(string extension) : IPlatformChecker
{
    public string Extension { get; } = extension;
    public string Id => extension.Replace(".", string.Empty);
    public string Name => extension;

    public async Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken)
    {
        var domain = $"{nickname}{Extension}";
        var url = Extension == ".com.br"
            ? $"https://registro.br/tecnologia/ferramentas/whois?search={domain}"
            : $"https://www.whois.com/whois/{domain}";

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain, cancellationToken);
            return addresses.Length > 0
                ? new(Name, url, CheckStatus.Occupied, ConfidenceLevel.Medium, "Domínio resolveu no DNS.")
                : new(Name, url, CheckStatus.Inconclusive, ConfidenceLevel.Low, "DNS sem resposta conclusiva.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SocketException)
        {
            return new(Name, url, CheckStatus.Available, ConfidenceLevel.Medium, "Domínio não resolveu no DNS.");
        }
        catch (Exception)
        {
            return new(Name, url, CheckStatus.Inconclusive, ConfidenceLevel.Low, "Consulta de domínio inconclusiva.");
        }
    }
}
```

Create `NameScan/Platforms/DotComDomainChecker.cs`:

```csharp
namespace NameScan.Platforms;

public sealed class DotComDomainChecker() : DomainPlatformChecker(".com");
```

Create `NameScan/Platforms/DotComBrDomainChecker.cs`:

```csharp
namespace NameScan.Platforms;

public sealed class DotComBrDomainChecker() : DomainPlatformChecker(".com.br");
```

- [ ] **Step 7: Add platform registry**

Create `NameScan/Platforms/PlatformRegistry.cs`:

```csharp
namespace NameScan.Platforms;

public sealed class PlatformRegistry(HttpClient httpClient) : IPlatformRegistry
{
    public IReadOnlyList<IPlatformChecker> GetAll() =>
    [
        new SocialPlatformChecker("instagram", "Instagram", nickname => new Uri($"https://www.instagram.com/{nickname}/"), httpClient),
        new SocialPlatformChecker("tiktok", "TikTok", nickname => new Uri($"https://www.tiktok.com/@{nickname}"), httpClient),
        new SocialPlatformChecker("x", "X", nickname => new Uri($"https://x.com/{nickname}"), httpClient),
        new SocialPlatformChecker("youtube", "YouTube", nickname => new Uri($"https://www.youtube.com/@{nickname}"), httpClient),
        new GitHubChecker(httpClient),
        new SocialPlatformChecker("twitch", "Twitch", nickname => new Uri($"https://www.twitch.tv/{nickname}"), httpClient),
        new DotComDomainChecker(),
        new DotComBrDomainChecker()
    ];
}
```

- [ ] **Step 8: Confirm platform code compiles**

Run:

```bash
dotnet build NameScan.sln
```

Expected: build succeeds with the new platform classes.

- [ ] **Step 9: Run platform tests**

Run:

```bash
dotnet test NameScan.sln --filter PlatformCheckerTests
```

Expected: all `PlatformCheckerTests` pass.

- [ ] **Step 10: Commit platform checkers**

Run:

```bash
git add NameScan/Platforms NameScan/Program.cs NameScan.Tests/Fakes.cs NameScan.Tests/Platforms
git commit -m "feat: add platform checkers"
```

Expected: commit succeeds.

---

### Task 5: Add Handle Check Orchestration

**Files:**
- Create: `NameScan/Features/Checks/CheckSummary.cs`
- Create: `NameScan/Features/Checks/CheckStreamEvent.cs`
- Create: `NameScan/Features/Checks/HandleCheckService.cs`
- Create: `NameScan.Tests/Features/Checks/HandleCheckServiceTests.cs`

- [ ] **Step 1: Write failing orchestration tests**

Create `NameScan.Tests/Features/Checks/HandleCheckServiceTests.cs`:

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NameScan.Features.Checks;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;

namespace NameScan.Tests.Features.Checks;

public sealed class HandleCheckServiceTests
{
    [Fact]
    public async Task StreamAsync_ReturnsInvalidEventForInvalidNickname()
    {
        var service = CreateService([new FakeChecker("GitHub", CheckStatus.Available)]);

        var events = await CollectAsync(service.StreamAsync("!", CancellationToken.None));

        var error = Assert.Single(events);
        Assert.Equal(CheckStreamEventKind.Error, error.Kind);
        Assert.Equal("Use apenas letras sem acento, números, ponto, underline ou hífen.", error.Message);
    }

    [Fact]
    public async Task StreamAsync_EmitsResultForEachCheckerAndDoneEvent()
    {
        var service = CreateService(
        [
            new FakeChecker("GitHub", CheckStatus.Available),
            new FakeChecker("Instagram", CheckStatus.Occupied)
        ]);

        var events = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));

        Assert.Equal(3, events.Count);
        Assert.Equal(CheckStreamEventKind.Result, events[0].Kind);
        Assert.Equal(CheckStreamEventKind.Result, events[1].Kind);
        Assert.Equal(CheckStreamEventKind.Done, events[2].Kind);
        Assert.Equal(1, events[2].Summary!.Available);
        Assert.Equal(1, events[2].Summary!.Occupied);
        Assert.Contains("minhamarcaapp", events[2].Suggestions);
    }

    [Fact]
    public async Task StreamAsync_IsolatesCheckerFailure()
    {
        var service = CreateService(
        [
            new ThrowingChecker("TikTok"),
            new FakeChecker("GitHub", CheckStatus.Available)
        ]);

        var events = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));

        var results = events.Where(item => item.Kind == CheckStreamEventKind.Result).Select(item => item.Result!).ToArray();

        Assert.Contains(results, result => result.Platform == "TikTok" && result.Status == CheckStatus.Error);
        Assert.Contains(results, result => result.Platform == "GitHub" && result.Status == CheckStatus.Available);
    }

    private static HandleCheckService CreateService(IReadOnlyList<IPlatformChecker> checkers) =>
        new(
            new NicknameValidator(),
            new FakeRegistry(checkers),
            new SuggestionService(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HandleCheckService>.Instance);

    private static async Task<List<CheckStreamEvent>> CollectAsync(IAsyncEnumerable<CheckStreamEvent> events)
    {
        var collected = new List<CheckStreamEvent>();
        await foreach (var streamEvent in events)
        {
            collected.Add(streamEvent);
        }

        return collected;
    }

    private sealed class FakeRegistry(IReadOnlyList<IPlatformChecker> checkers) : IPlatformRegistry
    {
        public IReadOnlyList<IPlatformChecker> GetAll() => checkers;
    }

    private sealed class FakeChecker(string name, CheckStatus status) : IPlatformChecker
    {
        public string Id => name.ToLowerInvariant();
        public string Name => name;

        public Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformCheckResult(Name, $"https://example.com/{nickname}", status, ConfidenceLevel.High, "Teste."));
    }

    private sealed class ThrowingChecker(string name) : IPlatformChecker
    {
        public string Id => name.ToLowerInvariant();
        public string Name => name;

        public Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Falha simulada.");
    }
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run:

```bash
dotnet test NameScan.sln --filter HandleCheckServiceTests
```

Expected: tests fail because orchestration types do not exist.

- [ ] **Step 3: Add stream event models**

Create `NameScan/Features/Checks/CheckSummary.cs`:

```csharp
namespace NameScan.Features.Checks;

public sealed record CheckSummary(
    int Available,
    int Occupied,
    int Invalid,
    int Inconclusive,
    int Error)
{
    public static CheckSummary From(IEnumerable<PlatformCheckResult> results)
    {
        var items = results.ToArray();
        return new CheckSummary(
            items.Count(item => item.Status == CheckStatus.Available),
            items.Count(item => item.Status == CheckStatus.Occupied),
            items.Count(item => item.Status == CheckStatus.Invalid),
            items.Count(item => item.Status == CheckStatus.Inconclusive),
            items.Count(item => item.Status == CheckStatus.Error));
    }
}
```

Create `NameScan/Features/Checks/CheckStreamEvent.cs`:

```csharp
namespace NameScan.Features.Checks;

public enum CheckStreamEventKind
{
    Result,
    Done,
    Error
}

public sealed record CheckStreamEvent(
    CheckStreamEventKind Kind,
    PlatformCheckResult? Result = null,
    string? Query = null,
    IReadOnlyList<string>? Suggestions = null,
    CheckSummary? Summary = null,
    string? Message = null)
{
    public static CheckStreamEvent ResultEvent(PlatformCheckResult result) =>
        new(CheckStreamEventKind.Result, Result: result);

    public static CheckStreamEvent DoneEvent(string query, IReadOnlyList<string> suggestions, CheckSummary summary) =>
        new(CheckStreamEventKind.Done, Query: query, Suggestions: suggestions, Summary: summary);

    public static CheckStreamEvent ErrorEvent(string message) =>
        new(CheckStreamEventKind.Error, Message: message);
}
```

- [ ] **Step 4: Add handle check service**

Create `NameScan/Features/Checks/HandleCheckService.cs`:

```csharp
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;

namespace NameScan.Features.Checks;

public sealed class HandleCheckService(
    NicknameValidator validator,
    IPlatformRegistry platformRegistry,
    SuggestionService suggestionService,
    IMemoryCache cache,
    ILogger<HandleCheckService> logger)
{
    private static readonly TimeSpan PlatformTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(2);

    public async IAsyncEnumerable<CheckStreamEvent> StreamAsync(
        string? nickname,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validation = validator.Validate(nickname);
        if (!validation.IsValid)
        {
            yield return CheckStreamEvent.ErrorEvent(validation.ErrorMessage!);
            yield break;
        }

        var normalized = validation.NormalizedNickname;
        var results = new ConcurrentBag<PlatformCheckResult>();
        var tasks = platformRegistry.GetAll().Select(checker => CheckWithCacheAsync(checker, normalized, cancellationToken)).ToList();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);

            var result = await completed;
            results.Add(result);

            logger.LogInformation(
                "NameScan platform result {Platform} {Status} {Confidence}",
                result.Platform,
                result.Status,
                result.Confidence);

            yield return CheckStreamEvent.ResultEvent(result);
        }

        var orderedResults = results.OrderBy(result => PlatformOrder(result.Platform)).ToArray();
        var suggestions = suggestionService.Generate(normalized);
        yield return CheckStreamEvent.DoneEvent(normalized, suggestions, CheckSummary.From(orderedResults));
    }

    private async Task<PlatformCheckResult> CheckWithCacheAsync(
        IPlatformChecker checker,
        string nickname,
        CancellationToken requestCancellationToken)
    {
        var cacheKey = $"check:{checker.Id}:{nickname}";
        if (cache.TryGetValue(cacheKey, out PlatformCheckResult? cached) && cached is not null)
        {
            return cached;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
        timeout.CancelAfter(PlatformTimeout);

        try
        {
            var result = await checker.CheckAsync(nickname, timeout.Token);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (OperationCanceledException) when (!requestCancellationToken.IsCancellationRequested)
        {
            return new PlatformCheckResult(checker.Name, string.Empty, CheckStatus.Inconclusive, ConfidenceLevel.Low, "Tempo limite atingido.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Platform checker failed for {Platform}", checker.Name);
            return new PlatformCheckResult(checker.Name, string.Empty, CheckStatus.Error, ConfidenceLevel.Low, "Falha técnica temporária.");
        }
    }

    private static int PlatformOrder(string platform) =>
        platform switch
        {
            "Instagram" => 0,
            "TikTok" => 1,
            "X" => 2,
            "YouTube" => 3,
            "GitHub" => 4,
            "Twitch" => 5,
            ".com" => 6,
            ".com.br" => 7,
            _ => 99
        };
}
```

- [ ] **Step 5: Run orchestration tests**

Run:

```bash
dotnet test NameScan.sln --filter HandleCheckServiceTests
```

Expected: all selected tests pass.

- [ ] **Step 6: Commit orchestration**

Run:

```bash
git add NameScan/Features/Checks NameScan.Tests/Features/Checks
git commit -m "feat: add progressive check orchestration"
```

Expected: commit succeeds.

---

### Task 6: Add SSE Endpoint

**Files:**
- Modify: `NameScan/Program.cs`
- Create: `NameScan.Tests/Web/CheckStreamEndpointTests.cs`

- [ ] **Step 1: Write failing SSE endpoint tests**

Create `NameScan.Tests/Web/CheckStreamEndpointTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

namespace NameScan.Tests.Web;

public sealed class CheckStreamEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CheckStreamEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsEventStreamContentType()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/check/stream?nickname=!", HttpCompletionOption.ResponseHeadersRead);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsErrorEventForInvalidNickname()
    {
        var client = _factory.CreateClient();

        var content = await client.GetStringAsync("/api/check/stream?nickname=!");

        Assert.Contains("event: error", content);
        Assert.Contains("Use apenas letras sem acento", content);
    }
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run:

```bash
dotnet test NameScan.sln --filter CheckStreamEndpointTests
```

Expected: tests fail because endpoint is not mapped.

- [ ] **Step 3: Add SSE endpoint**

Add these using statements to the top of `NameScan/Program.cs`:

```csharp
using System.Text.Json;
using NameScan.Features.Checks;
using NameScan.Features.Reporting;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;
```

Add these service registrations after `builder.Services.AddHttpClient();`:

```csharp
builder.Services.AddSingleton<NicknameValidator>();
builder.Services.AddSingleton<SuggestionService>();
builder.Services.AddSingleton<ReportFormatter>();
builder.Services.AddSingleton<IPlatformRegistry>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new PlatformRegistry(httpClientFactory.CreateClient());
});
builder.Services.AddScoped<HandleCheckService>();
```

In `NameScan/Program.cs`, insert this endpoint before `app.MapRazorComponents`:

```csharp
app.MapGet("/api/check/stream", async (
    string? nickname,
    HandleCheckService checkService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";
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

        var json = JsonSerializer.Serialize(streamEvent, JsonOptions);
        await httpContext.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
});
```

Add this local variable after `var app = builder.Build();`:

```csharp
var JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
```

- [ ] **Step 4: Run SSE tests**

Run:

```bash
dotnet test NameScan.sln --filter CheckStreamEndpointTests
```

Expected: endpoint tests pass.

- [ ] **Step 5: Run all backend tests**

Run:

```bash
dotnet test NameScan.sln
```

Expected: all tests pass.

- [ ] **Step 6: Commit SSE endpoint**

Run:

```bash
git add NameScan/Program.cs NameScan.Tests/Web/CheckStreamEndpointTests.cs
git commit -m "feat: stream check results with SSE"
```

Expected: commit succeeds.

---

### Task 7: Build MudBlazor Home UI

**Files:**
- Modify: `NameScan/Components/Pages/Home.razor`
- Modify: `NameScan/Components/Layout/MainLayout.razor`
- Modify: `NameScan/wwwroot/css/app.css`
- Create: `NameScan/wwwroot/js/check-stream.js`
- Create: `NameScan.Tests/Web/HomePageTests.cs`

- [ ] **Step 1: Write failing UI smoke test**

Create `NameScan.Tests/Web/HomePageTests.cs`:

```csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NameScan.Components.Pages;
using NameScan.Features.Reporting;

namespace NameScan.Tests.Web;

public sealed class HomePageTests : TestContext
{
    public HomePageTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<ReportFormatter>();
    }

    [Fact]
    public void Home_RendersBrazilFirstSearchExperience()
    {
        var component = RenderComponent<Home>();
        var markup = component.Markup;

        Assert.Contains("NameScan", markup);
        Assert.Contains("Verificar", markup);
        Assert.Contains("Apoia.se", markup);
        Assert.Contains("Os resultados são estimativas", markup);
    }
}
```

- [ ] **Step 2: Run UI test and confirm failure**

Run:

```bash
dotnet test NameScan.sln --filter HomePageTests
```

Expected: test fails because the page does not yet render the required MVP copy.

- [ ] **Step 3: Add EventSource bridge**

Create `NameScan/wwwroot/js/check-stream.js`:

```javascript
window.nameScanStream = {
  start: (nickname, dotNetRef) => {
    const source = new EventSource(`/api/check/stream?nickname=${encodeURIComponent(nickname)}`);

    source.addEventListener("result", async (event) => {
      await dotNetRef.invokeMethodAsync("OnStreamResult", event.data);
    });

    source.addEventListener("done", async (event) => {
      await dotNetRef.invokeMethodAsync("OnStreamDone", event.data);
      source.close();
    });

    source.addEventListener("error", async (event) => {
      if (event.data) {
        await dotNetRef.invokeMethodAsync("OnStreamError", event.data);
      }
      source.close();
    });

    return {
      stop: () => source.close()
    };
  }
};
```

Add the script in `NameScan/Components/App.razor` just before `</body>`:

```razor
<script src="_framework/blazor.web.js"></script>
<script src="js/check-stream.js"></script>
```

- [ ] **Step 4: Replace home page with MudBlazor UI**

Replace `NameScan/Components/Pages/Home.razor` with:

```razor
@page "/"
@rendermode InteractiveServer
@inject IJSRuntime JsRuntime
@inject ReportFormatter ReportFormatter

<PageTitle>NameScan</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="namescan-page">
    <MudStack Spacing="4">
        <MudStack Spacing="1">
            <MudText Typo="Typo.h3">NameScan</MudText>
            <MudText Typo="Typo.body1" Color="Color.Secondary">
                Verifique rapidamente se um nickname ou nome de marca parece disponível nas principais plataformas.
            </MudText>
        </MudStack>

        <MudAlert Severity="Severity.Info" Variant="Variant.Outlined">
            Os resultados são estimativas baseadas em verificações públicas. Confirme manualmente antes de tomar uma decisão final.
        </MudAlert>

        <MudPaper Class="namescan-search" Elevation="0">
            <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Start" Class="namescan-search-row">
                <MudTextField @bind-Value="_nickname"
                              Label="Nickname ou marca"
                              Placeholder="minhamarca"
                              Variant="Variant.Outlined"
                              Immediate="true"
                              Disabled="_isChecking"
                              Error="@(!string.IsNullOrWhiteSpace(_validationMessage))"
                              ErrorText="_validationMessage" />
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           Size="Size.Large"
                           Disabled="_isChecking"
                           OnClick="StartCheckAsync">
                    Verificar
                </MudButton>
            </MudStack>
        </MudPaper>

        @if (_isChecking)
        {
            <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
        }

        @if (_results.Count > 0 || _isChecking)
        {
            <MudTable Items="_results" Dense="true" Hover="true">
                <HeaderContent>
                    <MudTh>Plataforma</MudTh>
                    <MudTh>Status</MudTh>
                    <MudTh>Confiança</MudTh>
                    <MudTh>Observação</MudTh>
                    <MudTh>Link</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.Platform</MudTd>
                    <MudTd><MudChip T="string" Color="@StatusColor(context.Status)">@StatusLabel(context.Status)</MudChip></MudTd>
                    <MudTd>@ConfidenceLabel(context.Confidence)</MudTd>
                    <MudTd>@context.Note</MudTd>
                    <MudTd><MudLink Href="@context.Url" Target="_blank">Abrir</MudLink></MudTd>
                </RowTemplate>
            </MudTable>
        }

        @if (_suggestions.Count > 0)
        {
            <MudStack Spacing="2">
                <MudText Typo="Typo.h6">Sugestões alternativas</MudText>
                <MudStack Row="true" Spacing="1" Class="namescan-suggestions">
                    @foreach (var suggestion in _suggestions)
                    {
                        <MudChip T="string">@suggestion</MudChip>
                    }
                </MudStack>
            </MudStack>
        }

        @if (_results.Count > 0)
        {
            <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="CopyReportAsync">
                Copiar relatório
            </MudButton>
        }

        <MudDivider />
        <MudText Typo="Typo.body2">
            Este projeto é gratuito. Se ele te ajudou, considere apoiar pelo
            <MudLink Href="https://apoia.se/" Target="_blank">Apoia.se</MudLink>.
        </MudText>
    </MudStack>
</MudContainer>

@code {
    private string _nickname = string.Empty;
    private string? _validationMessage;
    private bool _isChecking;
    private readonly List<PlatformCheckResult> _results = [];
    private readonly List<string> _suggestions = [];

    private async Task StartCheckAsync()
    {
        _validationMessage = string.IsNullOrWhiteSpace(_nickname) ? "Digite um nickname para verificar." : null;
        if (_validationMessage is not null)
        {
            return;
        }

        _isChecking = true;
        _results.Clear();
        _suggestions.Clear();

        var reference = DotNetObjectReference.Create(this);
        await JsRuntime.InvokeVoidAsync("nameScanStream.start", _nickname, reference);
    }

    [JSInvokable]
    public Task OnStreamResult(string json)
    {
        var streamEvent = JsonSerializer.Deserialize<CheckStreamEvent>(json, JsonOptions);
        if (streamEvent?.Result is not null)
        {
            _results.Add(streamEvent.Result);
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnStreamDone(string json)
    {
        var streamEvent = JsonSerializer.Deserialize<CheckStreamEvent>(json, JsonOptions);
        _suggestions.Clear();
        _suggestions.AddRange(streamEvent?.Suggestions ?? []);
        _isChecking = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnStreamError(string json)
    {
        var streamEvent = JsonSerializer.Deserialize<CheckStreamEvent>(json, JsonOptions);
        _validationMessage = streamEvent?.Message ?? "Não foi possível iniciar a verificação.";
        _isChecking = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task CopyReportAsync()
    {
        var report = ReportFormatter.Format(_nickname, _results);
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", report);
    }

    private static Color StatusColor(CheckStatus status) =>
        status switch
        {
            CheckStatus.Available => Color.Success,
            CheckStatus.Occupied => Color.Error,
            CheckStatus.Invalid => Color.Warning,
            CheckStatus.Inconclusive => Color.Info,
            CheckStatus.Error => Color.Default,
            _ => Color.Default
        };

    private static string StatusLabel(CheckStatus status) => ReportFormatter.ToPortuguese(status);
    private static string ConfidenceLabel(ConfidenceLevel confidence) => ReportFormatter.ToPortuguese(confidence);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
```

Add these `@using` lines to `NameScan/Components/_Imports.razor`:

```razor
@using System.Text.Json
@using Microsoft.JSInterop
@using NameScan.Features.Checks
@using NameScan.Features.Reporting
@using NameScan.Features.Suggestions
@using NameScan.Platforms
@using NameScan.Validation
```

- [ ] **Step 5: Add responsive styling**

Replace `NameScan/wwwroot/css/app.css` with:

```css
:root {
  color-scheme: light;
}

html,
body {
  min-height: 100%;
}

body {
  margin: 0;
  background: #f7f9fb;
}

.namescan-page {
  padding-top: 48px;
  padding-bottom: 48px;
}

.namescan-search {
  padding: 20px;
  border: 1px solid #d9e2ec;
  border-radius: 8px;
  background: #ffffff;
}

.namescan-search-row {
  width: 100%;
}

.namescan-search-row .mud-input-control {
  flex: 1;
}

.namescan-suggestions {
  flex-wrap: wrap;
}

@media (max-width: 640px) {
  .namescan-page {
    padding-top: 24px;
    padding-bottom: 24px;
  }

  .namescan-search-row {
    flex-direction: column;
  }

  .namescan-search-row .mud-button-root {
    width: 100%;
  }
}
```

- [ ] **Step 6: Run UI test**

Run:

```bash
dotnet test NameScan.sln --filter HomePageTests
```

Expected: UI smoke test passes.

- [ ] **Step 7: Commit UI**

Run:

```bash
git add NameScan/Components NameScan/wwwroot NameScan.Tests/Web/HomePageTests.cs
git commit -m "feat: add MudBlazor search interface"
```

Expected: commit succeeds.

---

### Task 8: Add Observability And Final Verification

**Files:**
- Modify: `NameScan/Features/Checks/HandleCheckService.cs`
- Modify: `NameScan/Components/Pages/Home.razor`
- Modify: `README.md`

- [ ] **Step 1: Add user-event logging hooks**

In `NameScan/Components/Pages/Home.razor`, inject logger:

```razor
@inject ILogger<Home> Logger
```

Inside `StartCheckAsync`, after `_isChecking = true;`, add:

```csharp
Logger.LogInformation("NameScan search started");
```

Inside `CopyReportAsync`, after clipboard write, add:

```csharp
Logger.LogInformation("NameScan report copied");
```

- [ ] **Step 2: Add result-link logging helper**

In `NameScan/Components/Pages/Home.razor`, add this method:

```csharp
private void LogResultLinkClick(PlatformCheckResult result)
{
    Logger.LogInformation("NameScan result link clicked {Platform}", result.Platform);
}
```

Change the result link cell to:

```razor
<MudTd>
    <MudLink Href="@context.Url" Target="_blank" OnClick="@(() => LogResultLinkClick(context))">Abrir</MudLink>
</MudTd>
```

- [ ] **Step 3: Add Apoia.se click logging helper**

In `NameScan/Components/Pages/Home.razor`, add this method:

```csharp
private void LogSupportClick()
{
    Logger.LogInformation("NameScan support link clicked");
}
```

Change the Apoia.se link to:

```razor
<MudLink Href="https://apoia.se/" Target="_blank" OnClick="LogSupportClick">Apoia.se</MudLink>
```

- [ ] **Step 4: Update README**

Replace `README.md` with:

```markdown
# NameScan

NameScan é um web app gratuito para verificar se um nickname, handle ou nome de marca parece disponível em redes sociais, plataformas digitais e domínios.

O escopo inicial é Brasil primeiro. O MVP usa C# com ASP.NET Core, Blazor, MudBlazor e Server-Sent Events para mostrar resultados progressivamente.

## MVP

Alvos obrigatórios:

- Instagram
- TikTok
- X
- YouTube
- GitHub
- Twitch
- `.com`
- `.com.br`

Recursos principais:

- verificação progressiva via SSE;
- status e confiança por plataforma;
- sugestões alternativas simples;
- relatório copiável;
- aviso de limitação dos resultados;
- apoio voluntário via Apoia.se.

## Documentação

- Especificação de produto: `docs/superpowers/specs/2026-04-25-namescan-mvp-design.md`
- Plano de implementação: `docs/superpowers/plans/2026-04-25-namescan-mvp-implementation.md`
```

- [ ] **Step 5: Run full automated verification**

Run:

```bash
dotnet test NameScan.sln
dotnet build NameScan.sln
```

Expected: tests and build pass.

- [ ] **Step 6: Run the app locally**

Run:

```bash
dotnet run --project NameScan/NameScan.csproj
```

Expected: the app starts and prints a local URL such as `https://localhost:7xxx` or `http://localhost:5xxx`.

- [ ] **Step 7: Manual browser verification**

Open the local URL and verify:

```text
1. Home page renders NameScan, input, Verificar button, limitation notice, and Apoia.se link.
2. Empty input shows validation.
3. Searching "minhamarca" shows progressive result rows.
4. Each row has status, confidence, note, and Abrir link.
5. Suggestions appear after completion.
6. Copiar relatório copies plain text.
7. Layout remains readable on a mobile-width viewport.
```

- [ ] **Step 8: Commit final polish**

Run:

```bash
git add NameScan README.md
git commit -m "chore: verify MVP experience"
```

Expected: commit succeeds.

---

## Spec Coverage Review

- Brazil-first Portuguese experience: covered in Task 7 UI copy and Task 8 README.
- 8 required targets: covered in Task 4 `PlatformRegistry`.
- Hybrid verification: covered in Task 4 HTTP/domain mapping and Task 5 failure isolation.
- Progressive SSE: covered in Task 6 endpoint and Task 7 browser bridge.
- MudBlazor: covered in Task 1 package setup and Task 7 UI.
- Suggestions: covered in Task 3 and surfaced in Task 7.
- Copyable report: covered in Task 3 and Task 7.
- Apoia.se: covered in Task 7 and Task 8 logging.
- Cache and timeout: covered in Task 5 `HandleCheckService`.
- Tests: covered across Tasks 2 through 8.

## Execution Notes

- Work one task at a time and commit after each task.
- Do not call real social platforms from unit tests; use fake `HttpMessageHandler` responses.
- Treat `Inconclusive` as the default for ambiguous platform behavior.
- Keep user-facing strings in Portuguese.
- Keep code enums in English and translate labels at the UI/reporting boundary.
