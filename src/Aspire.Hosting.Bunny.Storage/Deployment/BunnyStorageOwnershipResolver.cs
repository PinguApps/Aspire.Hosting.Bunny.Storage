using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public enum BunnyStorageOwnershipResolutionAction
{
    Create,
    UseExisting,
}

public sealed record BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction Action, BunnyStorageZoneDetails? ExistingZone);

public static class BunnyStorageOwnershipResolver
{
    public static BunnyStorageOwnershipResolutionResult Resolve(
        BunnyStorageOwnershipMode ownershipMode,
        string storageZoneName,
        BunnyStorageZoneDetails? existingZone)
    {
        return ownershipMode switch
        {
            BunnyStorageOwnershipMode.CreateOnly when existingZone is not null =>
                throw new InvalidOperationException($"Bunny Storage zone '{storageZoneName}' already exists, but ownership mode is CreateOnly."),
            BunnyStorageOwnershipMode.CreateOnly => new(BunnyStorageOwnershipResolutionAction.Create, null),
            BunnyStorageOwnershipMode.ExistingOnly when existingZone is null =>
                throw new InvalidOperationException($"Bunny Storage zone '{storageZoneName}' does not exist, but ownership mode is ExistingOnly."),
            BunnyStorageOwnershipMode.ExistingOnly => new(BunnyStorageOwnershipResolutionAction.UseExisting, existingZone),
            BunnyStorageOwnershipMode.CreateOrAdopt when existingZone is null => new(BunnyStorageOwnershipResolutionAction.Create, null),
            BunnyStorageOwnershipMode.CreateOrAdopt => new(BunnyStorageOwnershipResolutionAction.UseExisting, existingZone),
            _ => throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Bunny Storage ownership mode is not supported."),
        };
    }
}
