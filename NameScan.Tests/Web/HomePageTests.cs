using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NameScan.Components.Pages;
using NameScan.Features.Reporting;

namespace NameScan.Tests.Web;

public sealed class HomePageTests : BunitContext
{
    public HomePageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ReportFormatter>();
    }

    [Fact]
    public void Home_RendersBrazilFirstSearchExperience()
    {
        var component = Render<Home>();
        var markup = component.Markup;

        Assert.Contains("NameScan", markup);
        Assert.Contains("Verificar", markup);
        Assert.Contains("Apoia.se", markup);
        Assert.Contains("Os resultados são estimativas", markup);
    }
}
