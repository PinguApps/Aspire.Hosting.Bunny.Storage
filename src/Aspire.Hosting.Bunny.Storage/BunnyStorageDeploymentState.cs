namespace Aspire.Hosting.Bunny.Storage;

public sealed class BunnyStorageDeploymentState
{
    public BunnyStorageDeploymentState(
        BunnyStorageValue storageZoneName,
        BunnyStorageValue apiKey,
        BunnyStorageOwnershipMode ownershipMode,
        BunnyStorageDeploymentOptions options)
    {
        ArgumentNullException.ThrowIfNull(storageZoneName);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(options);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Bunny Storage ownership mode is not supported.");
        }

        options.Validate();
        StorageZoneName = storageZoneName;
        ApiKey = apiKey;
        OwnershipMode = ownershipMode;
        OptionsSnapshot = new BunnyStorageDeploymentOptions(options);
    }

    public BunnyStorageValue StorageZoneName { get; }

    public BunnyStorageValue ApiKey { get; }

    public BunnyStorageOwnershipMode OwnershipMode { get; }

    public BunnyStorageDeploymentOptions Options => new(OptionsSnapshot);

    private BunnyStorageDeploymentOptions OptionsSnapshot { get; }
}
