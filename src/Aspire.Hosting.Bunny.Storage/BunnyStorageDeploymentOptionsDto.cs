namespace Aspire.Hosting.Bunny.Storage;

public sealed class BunnyStorageDeploymentOptionsDto
{
    public string OwnershipMode { get; set; } = nameof(BunnyStorageOwnershipMode.CreateOrAdopt);

    public BunnyStorageRegion Region { get; set; } = BunnyStorageRegion.De;

    public List<BunnyStorageRegion>? ReplicationRegions { get; set; }

    public bool CreatePullZone { get; set; }

    public string? PullZoneName { get; set; }

    public string? PublicBaseUrl { get; set; }

    internal BunnyStorageOwnershipMode GetOwnershipMode()
    {
        return Enum.TryParse(OwnershipMode, ignoreCase: true, out BunnyStorageOwnershipMode mode)
            ? mode
            : throw new InvalidOperationException($"Unknown Bunny Storage ownership mode '{OwnershipMode}'.");
    }

    internal BunnyStorageDeploymentOptions ToDeploymentOptions()
    {
        BunnyStorageDeploymentOptions options = new()
        {
            Region = Region,
            CreatePullZone = CreatePullZone,
            PullZoneName = PullZoneName,
            PublicBaseUrl = PublicBaseUrl,
        };
        if (ReplicationRegions is not null)
        {
            options.SetReplicationRegions([.. ReplicationRegions]);
        }

        return options;
    }
}
