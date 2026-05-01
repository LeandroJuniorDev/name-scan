# NameScan Home Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refatorar a Home do NameScan para um layout escuro e orientado a cards, fiel à referência visual, sem alterar o comportamento atual de busca, streaming, sugestões e cópia de relatório.

**Architecture:** A implementação concentra a mudança na composição de `Home.razor`, preservando a lógica existente e trocando apenas a camada de apresentação. O layout global recebe ajustes mínimos para AppBar, fundo e rodapé combinarem com a nova Home, enquanto os estilos finos ficam em `wwwroot/app.css`.

**Tech Stack:** ASP.NET Blazor, MudBlazor, bUnit, CSS

---

### Task 1: Atualizar os testes da Home para refletir o novo layout

**Files:**
- Modify: `NameScan.Tests/Web/HomePageTests.cs`
- Test: `NameScan.Tests/Web/HomePageTests.cs`

- [ ] **Step 1: Write the failing test expectations for the redesigned screen**

```csharp
[Fact]
public async Task Home_RendersHeroAndRealtimeResultsSections()
{
    await using var context = CreateContext();
    var component = context.Render<Home>();
    var markup = component.Markup;

    Assert.Contains("Verifique a disponibilidade do seu nome nas redes.", markup);
    Assert.Contains("Resultados em tempo real", markup);
    Assert.Contains("DOMÍNIOS", markup);
    Assert.Contains("SOCIAL", markup);
}
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `dotnet test /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.sln --filter Home_RendersHeroAndRealtimeResultsSections`
Expected: FAIL because the current Home still renders the old heading and table-oriented layout.

- [ ] **Step 3: Tighten one existing smoke test to the new copy**

```csharp
[Fact]
public async Task Home_RendersBrazilFirstSearchExperience()
{
    await using var context = CreateContext();
    var component = context.Render<Home>();
    var markup = component.Markup;

    Assert.Contains("NameScan", markup);
    Assert.Contains("ESCANEAR", markup);
    Assert.Contains("Verifique disponibilidade em Instagram", markup);
    Assert.Contains("Os resultados são informativos", markup);
}
```

- [ ] **Step 4: Run the Home page test file to capture the red state**

Run: `dotnet test /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.sln --filter NameScan.Tests.Web.HomePageTests`
Expected: FAIL in the updated render assertions, with the remaining behavioral tests still compiling.

- [ ] **Step 5: Commit**

```bash
git add /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.Tests/Web/HomePageTests.cs
git commit -m "test: cover redesigned home layout"
```

### Task 2: Refatorar layout global e a composição da Home sem mudar a lógica

**Files:**
- Modify: `NameScan/Components/Layout/MainLayout.razor`
- Modify: `NameScan/Components/Layout/MainLayout.razor.css`
- Modify: `NameScan/Components/Pages/Home.razor`

- [ ] **Step 1: Simplify the app shell to match the mock**

```razor
<MudLayout Class="namescan-shell">
    <MudAppBar Elevation="0" Dense="true" Class="namescan-appbar">
        <MudText Typo="Typo.h5" Class="namescan-brand">NameScan</MudText>
    </MudAppBar>

    <MudMainContent Class="main-content">
        @Body
    </MudMainContent>
</MudLayout>
```

- [ ] **Step 2: Replace the old page hierarchy with hero, section header, cards area, suggestions, copy action and footer**

```razor
<MudContainer MaxWidth="MaxWidth.False" Class="namescan-home">
    <section class="namescan-hero">
        <MudText Typo="Typo.h1" Class="namescan-hero-title">
            Verifique a disponibilidade do<br />seu nome nas redes.
        </MudText>

        <MudPaper Class="namescan-search-panel" Elevation="0">
            <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center" Class="namescan-search-row">
                <MudIcon Icon="@Icons.Material.Outlined.Search" Class="namescan-search-icon" />
                <MudTextField @bind-Value="_nickname" ... />
                <MudButton OnClick="StartCheckAsync" ...>ESCANEAR</MudButton>
            </MudStack>
        </MudPaper>

        <MudText Typo="Typo.body1" Class="namescan-hero-subtitle">
            Verifique disponibilidade em Instagram, TikTok, X, YouTube, GitHub, Twitch, .com e .com.br
        </MudText>
    </section>

    <section class="namescan-results-section">
        <div class="namescan-results-header">
            <MudText Typo="Typo.h4">Resultados em tempo real</MudText>
            <MudStack Row="true" Spacing="1">
                <MudChip T="string">DOMÍNIOS</MudChip>
                <MudChip T="string">SOCIAL</MudChip>
            </MudStack>
        </div>

        <div class="namescan-results-grid">
            @foreach (var result in _results)
            {
                <MudPaper Class="namescan-result-card" Elevation="0">
                    ...
                </MudPaper>
            }
        </div>
    </section>
</MudContainer>
```

- [ ] **Step 3: Add small helper methods in `Home.razor` for card rendering instead of changing the stream logic**

```csharp
private static string ResultTitle(PlatformCheckResult result) => result.Platform switch
{
    "GitHub" => string.IsNullOrWhiteSpace(result.Url) ? "github.com" : result.Url.Replace("https://", string.Empty),
    _ => string.IsNullOrWhiteSpace(result.Url) ? $"@{result.Platform.ToLowerInvariant()}" : result.Url.Replace("https://", string.Empty)
};

private static string ResultSubtitle(PlatformCheckResult result) =>
    string.IsNullOrWhiteSpace(result.Note) ? result.Platform : result.Note;

private static string ResultIcon(CheckStatus status) => status switch
{
    CheckStatus.Available => Icons.Material.Outlined.Language,
    CheckStatus.Occupied => Icons.Material.Outlined.AlternateEmail,
    CheckStatus.Inconclusive => Icons.Material.Outlined.SmartDisplay,
    CheckStatus.Invalid => Icons.Material.Outlined.ReportProblem,
    CheckStatus.Error => Icons.Material.Outlined.ErrorOutline,
    _ => Icons.Material.Outlined.Public
};
```

- [ ] **Step 4: Keep the existing actions visible in the new composition**

```razor
@if (_suggestions.Count > 0)
{
    <div class="namescan-suggestions-panel">
        @foreach (var suggestion in _suggestions)
        {
            <MudChip T="string" Class="namescan-suggestion-chip">@suggestion</MudChip>
        }
    </div>
}

@if (_results.Count > 0)
{
    <MudButton Variant="Variant.Outlined" OnClick="CopyReportAsync" Class="namescan-copy-button">
        Copiar relatório
    </MudButton>
}
```

- [ ] **Step 5: Run the Home page test file to verify the markup and behavior pass**

Run: `dotnet test /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.sln --filter NameScan.Tests.Web.HomePageTests`
Expected: PASS, confirming the new layout still supports validation, JS bootstrap failure, links, support click and copy report behavior.

- [ ] **Step 6: Commit**

```bash
git add /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/Components/Layout/MainLayout.razor /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/Components/Layout/MainLayout.razor.css /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/Components/Pages/Home.razor
git commit -m "feat: redesign namescan home structure"
```

### Task 3: Aplicar o acabamento visual e validar a solução ponta a ponta

**Files:**
- Modify: `NameScan/wwwroot/app.css`
- Test: `NameScan.Tests/Web/HomePageTests.cs`

- [ ] **Step 1: Add the dark theme surface and responsive layout styles**

```css
body {
    margin: 0;
    background: #17171b;
    color: #f3f3f6;
}

.namescan-home {
    padding: 0 0 5rem;
}

.namescan-result-card {
    border: 1px solid rgba(255, 255, 255, 0.05);
    background: #222226;
    border-radius: 10px;
}

@media (max-width: 640px) {
    .namescan-search-row {
        flex-direction: column;
        align-items: stretch;
    }
}
```

- [ ] **Step 2: Style the cards, chips, footer and search area to mirror the reference**

```css
.namescan-hero-title {
    font-size: clamp(2.6rem, 5vw, 4.4rem);
    font-weight: 800;
    text-align: center;
}

.namescan-status-chip.is-available {
    color: #39e0aa;
    border-color: rgba(57, 224, 170, 0.35);
}

.namescan-footer {
    border-top: 1px solid rgba(255, 255, 255, 0.08);
    color: #a9a9b3;
}
```

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.sln`
Expected: PASS for all existing test projects, confirming the redesign did not break app behavior.

- [ ] **Step 4: Review the diff for accidental behavior changes**

Run: `git diff -- /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/Components/Pages/Home.razor /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/wwwroot/app.css /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/Components/Layout/MainLayout.razor /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/Components/Layout/MainLayout.razor.css /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.Tests/Web/HomePageTests.cs`
Expected: only layout, styling and render helper changes, with no stream or service contract changes.

- [ ] **Step 5: Commit**

```bash
git add /Users/leandrodasilvajunior/git/personal/name-scan/NameScan/wwwroot/app.css /Users/leandrodasilvajunior/git/personal/name-scan/NameScan.Tests/Web/HomePageTests.cs
git commit -m "style: polish redesigned namescan home"
```
