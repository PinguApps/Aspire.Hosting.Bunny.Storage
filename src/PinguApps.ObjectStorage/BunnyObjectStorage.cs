using System.Net;
using System.Net.Http.Headers;

namespace PinguApps.ObjectStorage;

/// <summary>Bunny Storage HTTP API implementation of <see cref="IObjectStorage"/>.</summary>
public sealed class BunnyObjectStorage : IObjectStorage
{
    private readonly HttpClient _httpClient;
    private readonly string _storageZoneName;
    private readonly string _accessKey;
    private readonly Uri _endpoint;
    private readonly Uri _publicBaseUrl;

    /// <summary>Creates a Bunny Storage HTTP API object storage instance.</summary>
    public BunnyObjectStorage(
        HttpClient httpClient,
        string storageZoneName,
        string accessKey,
        string endpoint,
        string publicBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageZoneName);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicBaseUrl);

        _httpClient = httpClient;
        _storageZoneName = storageZoneName;
        _accessKey = accessKey;
        _endpoint = new Uri(endpoint.TrimEnd('/') + "/", UriKind.Absolute);
        _publicBaseUrl = new Uri(publicBaseUrl, UriKind.Absolute);
    }

    /// <inheritdoc />
    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        using HttpRequestMessage request = CreateRequest(HttpMethod.Put, key);
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = CreateRequest(HttpMethod.Get, key);
        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        request.Dispose();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, key);
        using HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, key);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public string GetPublicUrl(string key) => ObjectStorageKey.AppendEscaped(_publicBaseUrl, key);

    private HttpRequestMessage CreateRequest(HttpMethod method, string key)
    {
        string normalized = ObjectStorageKey.Normalize(key);
        string escapedKey = string.Join('/', normalized.Split('/').Select(Uri.EscapeDataString));
        Uri uri = new(_endpoint, $"{Uri.EscapeDataString(_storageZoneName)}/{escapedKey}");
        HttpRequestMessage request = new(method, uri);
        request.Headers.Add("AccessKey", _accessKey);
        return request;
    }
}
