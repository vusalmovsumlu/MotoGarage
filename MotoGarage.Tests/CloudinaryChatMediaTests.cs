using System.Net;
using Microsoft.Extensions.Configuration;
using MotoGarage.Infrastructure.Storage;

namespace MotoGarage.Tests;

public class CloudinaryChatMediaTests
{
    [Theory]
    [InlineData("image")]
    [InlineData("video")]
    public async Task DownloadUsesSignedAuthenticatedDeliveryWithoutExposingSecrets(string kind)
    {
        var transport = new Capture();
        using var http = new HttpClient(transport);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Cloudinary:CloudName"] = "test-account", ["Cloudinary:ApiKey"] = "test-key", ["Cloudinary:ApiSecret"] = "test-secret" }).Build();
        var service = new CloudinaryChatMediaStorage(config, http);
        Assert.Equal(new byte[] { 1, 2, 3 }, await service.ReadAsync("motogarage/chat/test", kind == "video" ? "mp4" : "png", kind, 100, default));
        Assert.Equal("res.cloudinary.com", transport.Uri!.Host);
        Assert.Equal("https", transport.Uri.Scheme);
        Assert.Contains($"/{kind}/authenticated/s--", transport.Uri.AbsolutePath);
        Assert.DoesNotContain("test-secret", transport.Uri.ToString());
        Assert.DoesNotContain("test-key", transport.Uri.ToString());
    }
    sealed class Capture : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Uri = request.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) }); }
    }
}
