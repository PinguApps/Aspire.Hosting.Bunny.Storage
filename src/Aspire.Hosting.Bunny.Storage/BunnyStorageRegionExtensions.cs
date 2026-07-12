namespace Aspire.Hosting.Bunny.Storage;

internal static class BunnyStorageRegionExtensions
{
    public static string ToProviderCode(this BunnyStorageRegion region)
    {
        if (region == BunnyStorageRegion.De)
        {
            return "DE";
        }

        if (region == BunnyStorageRegion.Ny)
        {
            return "NY";
        }

        if (region == BunnyStorageRegion.La)
        {
            return "LA";
        }

        if (region == BunnyStorageRegion.Sg)
        {
            return "SG";
        }

        if (region == BunnyStorageRegion.Syd)
        {
            return "SYD";
        }

        throw new ArgumentOutOfRangeException(nameof(region), region, "The Bunny Storage region is not supported.");
    }

    public static string GetStorageEndpoint(this BunnyStorageRegion region)
    {
        if (region == BunnyStorageRegion.De)
        {
            return "https://storage.bunnycdn.com";
        }

        if (region == BunnyStorageRegion.Ny)
        {
            return "https://ny.storage.bunnycdn.com";
        }

        if (region == BunnyStorageRegion.La)
        {
            return "https://la.storage.bunnycdn.com";
        }

        if (region == BunnyStorageRegion.Sg)
        {
            return "https://sg.storage.bunnycdn.com";
        }

        if (region == BunnyStorageRegion.Syd)
        {
            return "https://syd.storage.bunnycdn.com";
        }

        throw new ArgumentOutOfRangeException(nameof(region), region, "The Bunny Storage region is not supported.");
    }
}
