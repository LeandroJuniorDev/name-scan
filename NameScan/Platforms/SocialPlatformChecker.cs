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
