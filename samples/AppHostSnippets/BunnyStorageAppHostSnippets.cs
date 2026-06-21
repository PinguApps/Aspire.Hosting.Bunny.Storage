using Aspire.Hosting.Bunny.Storage;

namespace AppHostSnippets;

internal static class BunnyStorageAppHostSnippets
{
    public static void AddBunnyStorage(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> bunnyApiKey = builder.AddParameter("bunny-api-key", secret: true);

        IResourceBuilder<AzureBlobStorageContainerResource> media = builder.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media")
            .PublishToBunny("myapp-media", bunnyApiKey, options =>
            {
                options.Region = BunnyStorageRegion.De;
                options.CreatePullZone = true;
                options.PullZoneName = "myapp-media";
                options.PublicBaseUrl = "https://myapp-media.b-cdn.net";
            });

        builder.AddProject<Projects.Web>("web")
            .WithReference(media)
            .WithObjectStorage(media);
    }
}
