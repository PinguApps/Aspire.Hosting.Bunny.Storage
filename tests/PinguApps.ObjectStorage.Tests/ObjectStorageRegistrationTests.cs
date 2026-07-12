using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PinguApps.ObjectStorage.Tests;

public sealed class ObjectStorageRegistrationTests
{
    [Fact]
    public void AddObjectStorageRegistersAzureWhenConfigured()
    {
        IConfiguration configuration = BuildConfiguration(("ObjectStorage:media:Provider", "AzureBlob"),
            ("ObjectStorage:media:PublicBaseUrl", "http://127.0.0.1:10000/devstoreaccount1/media"),
            ("ObjectStorage:media:Azure:ConnectionString", "UseDevelopmentStorage=true"),
            ("ObjectStorage:media:Azure:ContainerName", "media"));

        ServiceProvider services = new ServiceCollection().AddObjectStorage(configuration).BuildServiceProvider();

        Assert.IsType<AzureBlobObjectStorage>(services.GetRequiredService<IObjectStorage>());
        Assert.Same(
            services.GetRequiredService<IObjectStorage>(),
            services.GetRequiredService<IObjectStorageProvider>().GetRequiredStorage("media"));
    }

    [Fact]
    public void AddObjectStorageRegistersBunnyWhenConfigured()
    {
        IConfiguration configuration = BuildConfiguration(("ObjectStorage:media:Provider", "Bunny"),
            ("ObjectStorage:media:PublicBaseUrl", "https://media.b-cdn.net"),
            ("ObjectStorage:media:Bunny:StorageZoneName", "zone"),
            ("ObjectStorage:media:Bunny:AccessKey", "storage-password"),
            ("ObjectStorage:media:Bunny:Endpoint", "https://storage.bunnycdn.com"));

        ServiceProvider services = new ServiceCollection().AddObjectStorage(configuration).BuildServiceProvider();

        Assert.IsType<BunnyObjectStorage>(services.GetRequiredService<IObjectStorage>());
    }

    [Fact]
    public void AddObjectStorageRejectsUnknownProvider()
    {
        IConfiguration configuration = BuildConfiguration(("ObjectStorage:media:Provider", "Other"));
        ServiceProvider services = new ServiceCollection().AddObjectStorage(configuration).BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IObjectStorage>());
        Assert.Contains("unknown provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddObjectStorageDoesNotRegisterBareStorageForMultipleStores()
    {
        IConfiguration configuration = BuildConfiguration(
            ("ObjectStorage:media:Provider", "AzureBlob"),
            ("ObjectStorage:media:PublicBaseUrl", "http://127.0.0.1:10000/devstoreaccount1/media"),
            ("ObjectStorage:media:Azure:ConnectionString", "UseDevelopmentStorage=true"),
            ("ObjectStorage:media:Azure:ContainerName", "media"),
            ("ObjectStorage:avatars:Provider", "AzureBlob"),
            ("ObjectStorage:avatars:PublicBaseUrl", "http://127.0.0.1:10000/devstoreaccount1/avatars"),
            ("ObjectStorage:avatars:Azure:ConnectionString", "UseDevelopmentStorage=true"),
            ("ObjectStorage:avatars:Azure:ContainerName", "avatars"));

        ServiceProvider services = new ServiceCollection().AddObjectStorage(configuration).BuildServiceProvider();

        Assert.Null(services.GetService<IObjectStorage>());
        Assert.IsType<AzureBlobObjectStorage>(services.GetRequiredService<IObjectStorageProvider>().GetRequiredStorage("avatars"));
    }

    [Fact]
    public async Task BunnyObjectStorageSendsAccessKeyAndEscapedStorageUrl()
    {
        CapturingHandler handler = new();
        BunnyObjectStorage storage = new(
            new HttpClient(handler),
            "my-zone",
            "storage-password",
            "https://ny.storage.bunnycdn.com",
            "https://cdn.example.com/assets");

        await storage.PutAsync("/folder/a b.txt", new MemoryStream([1, 2, 3]), "text/plain; charset=utf-8");

        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Equal("https://ny.storage.bunnycdn.com/my-zone/folder/a%20b.txt", handler.Requests[0].RequestUri.AbsoluteUri);
        Assert.Equal("storage-password", handler.Requests[0].AccessKey);
        Assert.Equal("text/plain; charset=utf-8", handler.Requests[0].ContentType);
        Assert.Equal("https://cdn.example.com/assets/folder/a%20b.txt", storage.GetPublicUrl("folder/a b.txt"));
    }

    [Fact]
    public async Task BunnyObjectStorageExistsUsesGetAndReturnsTrueWhenFileExists()
    {
        CapturingHandler handler = new();
        BunnyObjectStorage storage = new(
            new HttpClient(handler),
            "my-zone",
            "storage-password",
            "https://storage.bunnycdn.com",
            "https://cdn.example.com");

        bool exists = await storage.ExistsAsync("folder/file.png");

        Assert.True(exists);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://storage.bunnycdn.com/my-zone/folder/file.png", handler.Requests[0].RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task BunnyObjectStorageExistsReturnsFalseForNotFound()
    {
        CapturingHandler handler = new(HttpStatusCode.NotFound);
        BunnyObjectStorage storage = new(
            new HttpClient(handler),
            "my-zone",
            "storage-password",
            "https://storage.bunnycdn.com",
            "https://cdn.example.com");

        bool exists = await storage.ExistsAsync("missing.png");

        Assert.False(exists);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
    }

    [Fact]
    public async Task BunnyObjectStorageOpenReadDisposesResponseWhenReturnedStreamIsDisposed()
    {
        TrackingContent content = new([1, 2, 3]);
        CapturingHandler handler = new(content: content);
        BunnyObjectStorage storage = new(
            new HttpClient(handler),
            "my-zone",
            "storage-password",
            "https://storage.bunnycdn.com",
            "https://cdn.example.com");

        await using Stream stream = await storage.OpenReadAsync("folder/file.png");
        Assert.False(content.IsDisposed);

        await stream.DisposeAsync();

        Assert.True(content.IsDisposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://example.com/file.png")]
    [InlineData("../file.png")]
    [InlineData("folder/../file.png")]
    public void BunnyObjectStorageRejectsBadKeys(string key)
    {
        BunnyObjectStorage storage = new(new HttpClient(new CapturingHandler()), "zone", "key", "https://storage.bunnycdn.com", "https://cdn.example.com");

        Assert.ThrowsAny<ArgumentException>(() => storage.GetPublicUrl(key));
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly HttpContent? _content;

        public CapturingHandler(HttpStatusCode statusCode = HttpStatusCode.OK, HttpContent? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public List<RequestSnapshot> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(RequestSnapshot.Create(request));
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = _content ?? new ByteArrayContent([]),
            });
        }
    }

    private sealed record RequestSnapshot
    {
        public RequestSnapshot(
            HttpMethod method,
            Uri requestUri,
            string? accessKey,
            string? contentType)
        {
            Method = method;
            RequestUri = requestUri;
            AccessKey = accessKey;
            ContentType = contentType;
        }

        public HttpMethod Method { get; }

        public Uri RequestUri { get; }

        public string? AccessKey { get; }

        public string? ContentType { get; }

        public static RequestSnapshot Create(HttpRequestMessage request)
        {
            string? accessKey = request.Headers.TryGetValues("AccessKey", out IEnumerable<string>? values)
                ? values.SingleOrDefault()
                : null;

            return new RequestSnapshot(
                request.Method,
                request.RequestUri ?? throw new InvalidOperationException("Test request URI was missing."),
                accessKey,
                request.Content?.Headers.ContentType?.ToString());
        }
    }

    private sealed class TrackingContent : ByteArrayContent
    {
        public TrackingContent(byte[] content)
            : base(content)
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
