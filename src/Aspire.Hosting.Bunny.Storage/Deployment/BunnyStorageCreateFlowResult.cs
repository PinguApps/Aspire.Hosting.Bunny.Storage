using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed record BunnyStorageCreateFlowResult
{
    public BunnyStorageCreateFlowResult(
        BunnyStorageZoneDetails storageZone,
        BunnyPullZoneDetails? pullZone,
        bool created,
        BunnyStorageRemoteIdentityState remoteIdentity)
    {
        StorageZone = storageZone;
        PullZone = pullZone;
        Created = created;
        RemoteIdentity = remoteIdentity;
    }

    public BunnyStorageZoneDetails StorageZone { get; }

    public BunnyPullZoneDetails? PullZone { get; }

    public bool Created { get; }

    public BunnyStorageRemoteIdentityState RemoteIdentity { get; }
}
