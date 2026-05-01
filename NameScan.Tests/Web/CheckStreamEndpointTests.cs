using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Net;

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
        Assert.Equal("no-cache", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsKeepAliveConnectionHeaderForHttp11()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/check/stream?nickname=!")
        {
            Version = HttpVersion.Version11
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("keep-alive", response.Headers.Connection);
    }

    [Fact]
    public async Task StreamEndpoint_DoesNotReturnConnectionHeaderForHttp2()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/check/stream?nickname=!")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty(response.Headers.Connection);
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsErrorEventForInvalidNickname()
    {
        var client = _factory.CreateClient();

        var content = await client.GetStringAsync("/api/check/stream?nickname=!");

        Assert.Contains("event: error", content);
        Assert.Contains("data:", content);
        Assert.Contains("Use apenas letras sem acento", content);
    }

    [Theory]
    [InlineData("/_content/MudBlazor/MudBlazor.min.js")]
    [InlineData("/js/check-stream.js")]
    public async Task StaticAssets_ServeRequiredJavaScriptFiles(string path)
    {
        var client = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        using var response = await client.GetAsync(path);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }
}
