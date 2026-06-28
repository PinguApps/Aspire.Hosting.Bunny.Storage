namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed record BunnyStorageDeploymentProgress
{
    public BunnyStorageDeploymentProgress(
        string phase,
        string message,
        string? resourceName,
        string? storageZoneName,
        string? providerStorageZoneId)
    {
        Phase = phase;
        Message = message;
        ResourceName = resourceName;
        StorageZoneName = storageZoneName;
        ProviderStorageZoneId = providerStorageZoneId;
    }

    public string Phase { get; }

    public string Message { get; }

    public string? ResourceName { get; }

    public string? StorageZoneName { get; }

    public string? ProviderStorageZoneId { get; }
}
