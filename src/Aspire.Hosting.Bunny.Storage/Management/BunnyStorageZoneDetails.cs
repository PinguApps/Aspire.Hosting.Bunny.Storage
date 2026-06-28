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

    [JsonPropertyName("ReplicationRegions")]
    public List<string>? ReplicationRegions { get; set; }

    [JsonPropertyName("PullZones")]
    public List<BunnyPullZoneDetails>? PullZones { get; set; }
}
