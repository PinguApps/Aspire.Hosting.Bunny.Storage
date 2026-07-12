namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed record BunnyStorageRemoteIdentityState
{
    public BunnyStorageRemoteIdentityState(string storageZoneName, string providerStorageZoneId)
    {
        StorageZoneName = storageZoneName;
        ProviderStorageZoneId = providerStorageZoneId;
    }

    public string StorageZoneName { get; init; }

    public string ProviderStorageZoneId { get; init; }

    public void Deconstruct(out string storageZoneName, out string providerStorageZoneId)
    {
        storageZoneName = StorageZoneName;
        providerStorageZoneId = ProviderStorageZoneId;
    }
}
