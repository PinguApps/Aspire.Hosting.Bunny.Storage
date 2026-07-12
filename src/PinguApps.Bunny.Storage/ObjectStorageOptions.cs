namespace PinguApps.Bunny.Storage;

/// <summary>Configuration for one named object storage registration.</summary>
public sealed class ObjectStorageOptions
{
    /// <summary>Provider name. Supported values: AzureBlob, Bunny.</summary>
    public string? Provider { get; set; }

    /// <summary>Base URL used by <see cref="IObjectStorage.GetPublicUrl"/>.</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Azure Blob provider settings.</summary>
    public AzureBlobObjectStorageOptions Azure { get; set; } = new();

    /// <summary>Bunny Storage provider settings.</summary>
    public BunnyObjectStorageOptions Bunny { get; set; } = new();
}

/// <summary>Azure Blob provider settings.</summary>
public sealed class AzureBlobObjectStorageOptions
{
    /// <summary>Azure Blob Storage connection string.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Blob container name.</summary>
    public string? ContainerName { get; set; }
}

/// <summary>Bunny Storage provider settings.</summary>
public sealed class BunnyObjectStorageOptions
{
    /// <summary>Bunny storage zone name.</summary>
    public string? StorageZoneName { get; set; }

    /// <summary>Bunny storage zone password/access key.</summary>
    public string? AccessKey { get; set; }

    /// <summary>Bunny Storage HTTP API endpoint.</summary>
    public string? Endpoint { get; set; }
}
