using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PinguApps.Bunny.Storage;

/// <summary>Azure Blob Storage implementation of <see cref="IObjectStorage"/>.</summary>
public sealed class AzureBlobObjectStorage : IObjectStorage
{
    private readonly BlobContainerClient _container;
    private readonly Uri _publicBaseUrl;

    /// <summary>Creates an Azure Blob object storage instance.</summary>
    public AzureBlobObjectStorage(string connectionString, string containerName, string? publicBaseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        _container = new BlobContainerClient(SanitizeConnectionString(connectionString), containerName);
        _publicBaseUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
            ? _container.Uri
            : new Uri(publicBaseUrl, UriKind.Absolute);
    }

    /// <inheritdoc />
    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        string normalized = ObjectStorageKey.Normalize(key);
        await EnsurePublicContainerAsync(cancellationToken).ConfigureAwait(false);
        BlobClient blob = _container.GetBlobClient(normalized);
        BlobUploadOptions options = new()
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };

        await blob.UploadAsync(content, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        string normalized = ObjectStorageKey.Normalize(key);
        BlobClient blob = _container.GetBlobClient(normalized);
        return await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        string normalized = ObjectStorageKey.Normalize(key);
        BlobClient blob = _container.GetBlobClient(normalized);
        return await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        string normalized = ObjectStorageKey.Normalize(key);
        BlobClient blob = _container.GetBlobClient(normalized);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string GetPublicUrl(string key) => ObjectStorageKey.AppendEscaped(_publicBaseUrl, key);

    private async Task EnsurePublicContainerAsync(CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken).ConfigureAwait(false);

        BlobContainerAccessPolicy accessPolicy = await _container.GetAccessPolicyAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (accessPolicy.BlobPublicAccess != PublicAccessType.Blob)
        {
            await _container.SetAccessPolicyAsync(
                PublicAccessType.Blob,
                accessPolicy.SignedIdentifiers,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private static string SanitizeConnectionString(string connectionString)
    {
        return string.Join(
            ';',
            connectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => !part.StartsWith("ContainerName=", StringComparison.OrdinalIgnoreCase)));
    }
}
