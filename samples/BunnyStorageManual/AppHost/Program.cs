using Aspire.Hosting.Bunny.Storage;
using Aspire.Hosting.Azure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> bunnyApiKey = builder.AddParameter("bunny-api-key", secret: true);

IResourceBuilder<AzureBlobStorageContainerResource> media = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobContainer("media", "media")
    .PublishToBunny(
        storageZoneName: "myapp-media",
        apiKey: bunnyApiKey,
        ownershipMode: BunnyStorageOwnershipMode.CreateOrAdopt,
        options =>
        {
            options.Region = BunnyStorageRegion.De;
            options.SetReplicationRegions(BunnyStorageRegion.Ny);
            options.CreatePullZone = true;
            options.PullZoneName = "myapp-media";
            options.PublicBaseUrl = "https://myapp-media.b-cdn.net";
        });

builder.AddProject<Projects.BunnyStorageManual_Web>("web")
    .WithReference(media)
    .WithObjectStorage(media);

builder.Build().Run();
