namespace Aspire.Hosting.Bunny.Storage.Management;

public sealed record BunnyStorageManagementCredentials
{
    public BunnyStorageManagementCredentials(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ApiKey = apiKey;
    }

    public string ApiKey { get; }
}
