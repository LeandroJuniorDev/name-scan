using System.Net;
using System.Net.Sockets;
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public abstract class DomainPlatformChecker : IPlatformChecker
{
    protected DomainPlatformChecker(string extension)
    {
        Extension = extension;
    }

    public string Extension { get; }
    public string Id => Extension.Replace(".", string.Empty);
    public string Name => Extension;

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
