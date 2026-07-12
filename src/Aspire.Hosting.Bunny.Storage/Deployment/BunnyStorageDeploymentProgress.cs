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

    public string Phase { get; init; }

    public string Message { get; init; }

    public string? ResourceName { get; init; }

    public string? StorageZoneName { get; init; }

    public string? ProviderStorageZoneId { get; init; }

    public void Deconstruct(
        out string phase,
        out string message,
        out string? resourceName,
        out string? storageZoneName,
        out string? providerStorageZoneId)
    {
        phase = Phase;
        message = Message;
        resourceName = ResourceName;
        storageZoneName = StorageZoneName;
        providerStorageZoneId = ProviderStorageZoneId;
    }
}
