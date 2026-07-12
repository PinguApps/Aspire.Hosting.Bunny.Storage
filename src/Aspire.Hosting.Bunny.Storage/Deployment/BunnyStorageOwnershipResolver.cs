using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public enum BunnyStorageOwnershipResolutionAction
{
    Create,
    UseExisting,
}

public sealed record BunnyStorageOwnershipResolutionResult
{
    public BunnyStorageOwnershipResolutionResult(
        BunnyStorageOwnershipResolutionAction action,
        BunnyStorageZoneDetails? existingZone)
    {
        Action = action;
        ExistingZone = existingZone;
    }

    public BunnyStorageOwnershipResolutionAction Action { get; init; }

    public BunnyStorageZoneDetails? ExistingZone { get; init; }

    public void Deconstruct(
        out BunnyStorageOwnershipResolutionAction action,
        out BunnyStorageZoneDetails? existingZone)
    {
        action = Action;
        existingZone = ExistingZone;
    }
}

public static class BunnyStorageOwnershipResolver
{
    public static BunnyStorageOwnershipResolutionResult Resolve(
        BunnyStorageOwnershipMode ownershipMode,
        string storageZoneName,
        BunnyStorageZoneDetails? existingZone,
        bool existingZoneResolvedFromCachedIdentity = false)
    {
        if (ownershipMode == BunnyStorageOwnershipMode.CreateOnly)
        {
            if (existingZone is not null && existingZoneResolvedFromCachedIdentity)
            {
                return new BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction.UseExisting, existingZone);
            }

            if (existingZone is not null)
            {
                throw new InvalidOperationException($"Bunny Storage zone '{storageZoneName}' already exists, but ownership mode is CreateOnly.");
            }

            return new BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction.Create, null);
        }

        if (ownershipMode == BunnyStorageOwnershipMode.ExistingOnly)
        {
            if (existingZone is null)
            {
                throw new InvalidOperationException($"Bunny Storage zone '{storageZoneName}' does not exist, but ownership mode is ExistingOnly.");
            }

            return new BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction.UseExisting, existingZone);
        }

        if (ownershipMode == BunnyStorageOwnershipMode.CreateOrAdopt)
        {
            if (existingZone is null)
            {
                return new BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction.Create, null);
            }

            return new BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction.UseExisting, existingZone);
        }

        throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Bunny Storage ownership mode is not supported.");
    }
}
