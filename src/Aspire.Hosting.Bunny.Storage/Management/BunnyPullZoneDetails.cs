using System.Text.Json.Serialization;

namespace Aspire.Hosting.Bunny.Storage.Management;

public sealed class BunnyPullZoneDetails
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("OriginUrl")]
    public string? OriginUrl { get; set; }

    [JsonPropertyName("StorageZoneId")]
    public long? StorageZoneId { get; set; }
}
