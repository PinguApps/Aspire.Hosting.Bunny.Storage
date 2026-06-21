namespace PinguApps.ObjectStorage;

/// <summary>Supported runtime storage providers.</summary>
public enum ObjectStorageProviderKind
{
    /// <summary>Azure Blob Storage.</summary>
    AzureBlob,

    /// <summary>Bunny Storage HTTP API.</summary>
    Bunny,
}
