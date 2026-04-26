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
