namespace Aspire.Hosting.Bunny.Storage;

[AspireDto]
public sealed class BunnyStorageDeploymentOptionsDto
{
    public BunnyStorageOwnershipMode? OwnershipMode { get; set; }

    public BunnyStorageRegion? Region { get; set; }

#pragma warning disable CA1819
    public BunnyStorageRegion[]? ReplicationRegions { get; set; }
#pragma warning restore CA1819

    public bool? CreatePullZone { get; set; }

    public string? PullZoneName { get; set; }

    public string? PublicBaseUrl { get; set; }

    internal BunnyStorageOwnershipMode GetOwnershipMode()
    {
        return OwnershipMode ?? BunnyStorageOwnershipMode.CreateOrAdopt;
    }

    internal BunnyStorageDeploymentOptions ToDeploymentOptions()
    {
        BunnyStorageDeploymentOptions options = new();
        if (Region is not null)
        {
            options.Region = Region.Value;
        }

        if (CreatePullZone is not null)
        {
            options.CreatePullZone = CreatePullZone.Value;
        }

        if (PullZoneName is not null)
        {
            options.PullZoneName = PullZoneName;
        }

        if (PublicBaseUrl is not null)
        {
            options.PublicBaseUrl = PublicBaseUrl;
        }

        if (ReplicationRegions is not null)
        {
            options.SetReplicationRegions(ReplicationRegions);
        }

        return options;
    }
}
