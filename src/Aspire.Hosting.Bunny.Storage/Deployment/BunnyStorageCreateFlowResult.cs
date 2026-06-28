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

    public BunnyStorageZoneDetails StorageZone { get; init; }

    public BunnyPullZoneDetails? PullZone { get; init; }

    public bool Created { get; init; }

    public BunnyStorageRemoteIdentityState RemoteIdentity { get; init; }

    public void Deconstruct(
        out BunnyStorageZoneDetails storageZone,
        out BunnyPullZoneDetails? pullZone,
        out bool created,
        out BunnyStorageRemoteIdentityState remoteIdentity)
    {
        storageZone = StorageZone;
        pullZone = PullZone;
        created = Created;
        remoteIdentity = RemoteIdentity;
    }
}
