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
