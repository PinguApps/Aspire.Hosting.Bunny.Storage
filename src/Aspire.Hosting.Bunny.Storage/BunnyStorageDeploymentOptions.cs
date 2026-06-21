namespace Aspire.Hosting.Bunny.Storage;

public sealed class BunnyStorageDeploymentOptions
{
    private readonly HashSet<string> _explicitSettings;
    private readonly List<BunnyStorageRegion> _replicationRegions = [];

    public BunnyStorageDeploymentOptions()
    {
        _explicitSettings = new HashSet<string>(StringComparer.Ordinal);
    }

    internal BunnyStorageDeploymentOptions(BunnyStorageDeploymentOptions source)
    {
        _explicitSettings = [.. source._explicitSettings];
        Region = source.Region;
        CreatePullZone = source.CreatePullZone;
        PullZoneName = source.PullZoneName;
        PublicBaseUrl = source.PublicBaseUrl;
        _replicationRegions.AddRange(source._replicationRegions);
    }

    public BunnyStorageRegion Region
    {
        get => field;
        set
        {
            field = value;
            _explicitSettings.Add(nameof(Region));
        }
    } = BunnyStorageRegion.De;

    public bool CreatePullZone
    {
        get => field;
        set
        {
            field = value;
            _explicitSettings.Add(nameof(CreatePullZone));
        }
    }

    public string? PullZoneName
    {
        get => field;
        set
        {
            field = value;
            _explicitSettings.Add(nameof(PullZoneName));
        }
    }

    public string? PublicBaseUrl
    {
        get => field;
        set
        {
            field = value;
            _explicitSettings.Add(nameof(PublicBaseUrl));
        }
    }

    public IReadOnlyList<BunnyStorageRegion> ReplicationRegions => _replicationRegions;

    internal IReadOnlySet<string> ExplicitSettings => _explicitSettings;

    public void SetReplicationRegions(params BunnyStorageRegion[] regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        _replicationRegions.Clear();
        _replicationRegions.AddRange(regions);
        _explicitSettings.Add(nameof(ReplicationRegions));
    }

    internal void Validate()
    {
        if (!Enum.IsDefined(Region))
        {
            throw new InvalidOperationException("The Bunny Storage primary region is not supported.");
        }

        foreach (BunnyStorageRegion region in _replicationRegions)
        {
            if (!Enum.IsDefined(region))
            {
                throw new InvalidOperationException("A Bunny Storage replication region is not supported.");
            }

            if (region == Region)
            {
                throw new InvalidOperationException("Bunny Storage replication regions must not include the primary region.");
            }
        }

        if (CreatePullZone && string.IsNullOrWhiteSpace(PullZoneName))
        {
            throw new InvalidOperationException("Bunny Storage pull-zone creation requires PullZoneName.");
        }

        if (!CreatePullZone && string.IsNullOrWhiteSpace(PublicBaseUrl))
        {
            throw new InvalidOperationException("Bunny Storage deployment requires either CreatePullZone or PublicBaseUrl so deployed apps receive a public read URL.");
        }

        if (!string.IsNullOrWhiteSpace(PublicBaseUrl)
            && !Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Bunny Storage PublicBaseUrl must be an absolute URL.");
        }
    }
}
