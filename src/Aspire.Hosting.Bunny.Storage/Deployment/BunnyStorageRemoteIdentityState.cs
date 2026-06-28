namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed record BunnyStorageRemoteIdentityState
{
    public BunnyStorageRemoteIdentityState(string storageZoneName, string providerStorageZoneId)
    {
        StorageZoneName = storageZoneName;
        ProviderStorageZoneId = providerStorageZoneId;
    }

    public string StorageZoneName { get; }

    public string ProviderStorageZoneId { get; }
}
