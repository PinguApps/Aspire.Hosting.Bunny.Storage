# Aspire.Hosting.Bunny.Storage

Two packages:

- `Aspire.Hosting.Bunny.Storage`: Aspire AppHost integration for deploying Bunny Storage.
- `PinguApps.ObjectStorage`: app runtime abstraction over Azure Blob Storage and Bunny Storage.

Local development uses Aspire Azure Blob Storage with Azurite. Deployed infrastructure uses Bunny Storage and, optionally, a Bunny Pull Zone CDN. App code uses only `IObjectStorage`.

```csharp
var bunnyApiKey = builder.AddParameter("bunny-api-key", secret: true);

var media = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobContainer("media", "media")
    .PublishToBunny("myapp-media", bunnyApiKey, configure: options =>
    {
        options.Region = BunnyStorageRegion.De;
        options.CreatePullZone = true;
        options.PullZoneName = "myapp-media";
        options.PublicBaseUrl = "https://myapp-media.b-cdn.net";
    });

builder.AddProject<Projects.Web>("web")
    .WithReference(media)
    .WithObjectStorage(media);
```

Runtime:

```csharp
builder.Services.AddObjectStorage(builder.Configuration);
```

Store object keys such as `uploads/avatar.png` in your database. Do not store absolute URLs. Bunny write credentials are server-side only.
