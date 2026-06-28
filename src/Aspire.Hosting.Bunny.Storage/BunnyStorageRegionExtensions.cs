namespace Aspire.Hosting.Bunny.Storage;

internal static class BunnyStorageRegionExtensions
{
    public static string ToProviderCode(this BunnyStorageRegion region)
    {
        return region switch
        {
            BunnyStorageRegion.De => "DE",
            BunnyStorageRegion.Ny => "NY",
            BunnyStorageRegion.La => "LA",
            BunnyStorageRegion.Sg => "SG",
            BunnyStorageRegion.Syd => "SYD",
            _ => throw new ArgumentOutOfRangeException(nameof(region), region, "The Bunny Storage region is not supported."),
        };
    }

    public static string GetStorageEndpoint(this BunnyStorageRegion region)
    {
        return region switch
        {
            BunnyStorageRegion.De => "https://storage.bunnycdn.com",
            BunnyStorageRegion.Ny => "https://ny.storage.bunnycdn.com",
            BunnyStorageRegion.La => "https://la.storage.bunnycdn.com",
            BunnyStorageRegion.Sg => "https://sg.storage.bunnycdn.com",
            BunnyStorageRegion.Syd => "https://syd.storage.bunnycdn.com",
            _ => throw new ArgumentOutOfRangeException(nameof(region), region, "The Bunny Storage region is not supported."),
        };
    }
}
