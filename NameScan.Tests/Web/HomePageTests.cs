using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NameScan.Components.Pages;
using NameScan.Features.Checks;
using NameScan.Features.Reporting;
using System.Reflection;
using System.Text.Json;

namespace NameScan.Tests.Web;

public sealed class HomePageTests
{
    [Fact]
    public async Task Home_RendersBrazilFirstSearchExperience()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();
        var markup = component.Markup;

        Assert.Contains("NameScan", markup);
        Assert.Contains("Verificar", markup);
        Assert.Contains("Apoia.se", markup);
        Assert.Contains("Os resultados são estimativas", markup);
    }

    [Fact]
    public async Task Home_StopsLoadingAndShowsFallbackMessage_WhenStreamErrorsWithoutPayload()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();

        component.Find("input").Input("minhamarca");
        await component.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Contains("mud-progress-linear", component.Markup);

        await component.InvokeAsync(() => component.Instance.OnStreamError("{}"));

        Assert.DoesNotContain("mud-progress-linear", component.Markup);
        Assert.Equal("Não foi possível concluir a verificação.", GetPrivateField<string?>(component.Instance, "_validationMessage"));
    }

    [Fact]
    public async Task Home_DoesNotRenderOpenLink_WhenResultUrlIsEmpty()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();
        var payload = JsonSerializer.Serialize(
            CheckStreamEvent.ResultEvent(
                new PlatformCheckResult(
                    "GitHub",
                    string.Empty,
                    CheckStatus.Available,
                    ConfidenceLevel.High,
                    "Disponível")));

        await component.InvokeAsync(() => component.Instance.OnStreamResult(payload));

        Assert.DoesNotContain(">Abrir</a>", component.Markup);
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddSingleton<ReportFormatter>();
        return context;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field.GetValue(instance)!;
    }
}
