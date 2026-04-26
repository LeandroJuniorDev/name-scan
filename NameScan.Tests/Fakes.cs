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
