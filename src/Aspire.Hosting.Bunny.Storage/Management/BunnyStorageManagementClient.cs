using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.Bunny.Storage.Management;

public sealed class BunnyStorageManagementClient : IBunnyStorageManagementClient
{
    private static readonly JsonSerializerOptions _readJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions _writeJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly BunnyStorageManagementCredentials _credentials;

    public BunnyStorageManagementClient(HttpClient httpClient, BunnyStorageManagementCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(credentials);
        if (httpClient.BaseAddress is null || !httpClient.BaseAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("Bunny Storage management client requires an absolute HttpClient.BaseAddress.", nameof(httpClient));
        }

        _httpClient = httpClient;
        _credentials = credentials;
    }

    public async Task<IReadOnlyList<BunnyStorageZoneDetails>> ListStorageZonesAsync(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, "storagezone");
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<List<BunnyStorageZoneDetails>>(_readJsonOptions, cancellationToken).ConfigureAwait(false)
            ?? [];
    }

    public async Task<BunnyStorageZoneDetails> CreateStorageZoneAsync(
        string name,
        string region,
        IReadOnlyList<string> replicationRegions,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, "storagezone");
        request.Content = JsonContent.Create(new
        {
            Name = name,
            Region = region,
            ReplicationRegions = replicationRegions,
        }, options: _writeJsonOptions);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<BunnyStorageZoneDetails>(_readJsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new BunnyStorageProviderException(BunnyStorageProviderFailureKind.Unexpected, "Bunny returned an empty storage-zone response.");
    }

    public async Task<IReadOnlyList<BunnyPullZoneDetails>> ListPullZonesAsync(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, "pullzone");
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<List<BunnyPullZoneDetails>>(_readJsonOptions, cancellationToken).ConfigureAwait(false)
            ?? [];
    }

    public async Task<BunnyPullZoneDetails> CreatePullZoneAsync(
        string name,
        string originUrl,
        long storageZoneId,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, "pullzone");
        request.Content = JsonContent.Create(new
        {
            Name = name,
            OriginUrl = originUrl,
            StorageZoneId = storageZoneId,
        }, options: _writeJsonOptions);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(body))
        {
            return JsonSerializer.Deserialize<BunnyPullZoneDetails>(body, _readJsonOptions)
                ?? throw new BunnyStorageProviderException(BunnyStorageProviderFailureKind.Unexpected, "Bunny returned an invalid pull-zone response.");
        }

        IReadOnlyList<BunnyPullZoneDetails> pullZones = await ListPullZonesAsync(cancellationToken).ConfigureAwait(false);
        return pullZones.FirstOrDefault(zone => string.Equals(zone.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? new BunnyPullZoneDetails
            {
                Name = name,
                OriginUrl = originUrl,
                StorageZoneId = storageZoneId,
            };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Add("AccessKey", _credentials.ApiKey);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (IsStorageZoneBeingDeleted(response.StatusCode, body))
        {
            throw new BunnyStorageProviderException(
                BunnyStorageProviderFailureKind.StorageZoneBeingDeleted,
                "Bunny Storage zone name is still being deleted and cannot be recreated yet. Wait for Bunny to release the name, then retry deployment.");
        }

        BunnyStorageProviderFailureKind kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => BunnyStorageProviderFailureKind.Authentication,
            HttpStatusCode.NotFound => BunnyStorageProviderFailureKind.NotFound,
            HttpStatusCode.Conflict => BunnyStorageProviderFailureKind.Conflict,
            HttpStatusCode.BadRequest => BunnyStorageProviderFailureKind.Validation,
            _ => BunnyStorageProviderFailureKind.Unexpected,
        };
        throw new BunnyStorageProviderException(kind, $"Bunny API request failed with {(int)response.StatusCode}: {body}");
    }

    private static bool IsStorageZoneBeingDeleted(HttpStatusCode statusCode, string body)
    {
        return statusCode == HttpStatusCode.BadRequest
            && body.Contains("\"ErrorKey\":\"storagezone.name_taken\"", StringComparison.OrdinalIgnoreCase)
            && body.Contains("currently being deleted", StringComparison.OrdinalIgnoreCase);
    }
}
