using System.Text.Json.Serialization;

namespace Aspire.Hosting.Bunny.Storage.Management;

public sealed class BunnyStorageZoneDetails
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("Region")]
    public string Region { get; set; } = string.Empty;

#pragma warning disable CA1819
    [JsonPropertyName("ReplicationRegions")]
    public string[]? ReplicationRegions { get; set; }

    [JsonPropertyName("PullZones")]
    public BunnyPullZoneDetails[]? PullZones { get; set; }
#pragma warning restore CA1819
}
