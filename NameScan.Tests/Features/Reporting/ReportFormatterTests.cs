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
