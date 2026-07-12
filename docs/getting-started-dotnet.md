# C# AppHost Usage

Install the hosting and server runtime packages:

```powershell
dotnet add package PinguApps.Aspire.Hosting.Bunny.Storage
dotnet add package PinguApps.ObjectStorage
```

AppHost:

```csharp
using Aspire.Hosting.Bunny.Storage;

var builder = DistributedApplication.CreateBuilder(args);
var bunnyApiKey = builder.AddParameter("bunny-api-key", secret: true);

var media = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobContainer("media", "media")
    .PublishToBunny(
        "myapp-media",
        bunnyApiKey,
        BunnyStorageOwnershipMode.CreateOrAdopt,
        options =>
        {
            options.Region = BunnyStorageRegion.De;
            options.SetReplicationRegions(BunnyStorageRegion.Ny);
            options.CreatePullZone = true;
            options.PullZoneName = "myapp-media";
        });

builder.AddProject<Projects.Web>("web")
    .WithReference(media)
    .WithObjectStorage(media);

builder.Build().Run();
```

Call `WithObjectStorage` after `WithReference` and `WaitFor` calls involving the same Azure container. This lets the integration remove Azure-only relationships during deployment.

Server application:

```csharp
using PinguApps.ObjectStorage;

builder.Services.AddObjectStorage(builder.Configuration);
```

```csharp
await storage.PutAsync("images/example.png", stream, "image/png", cancellationToken);
string url = storage.GetPublicUrl("images/example.png");
```

The maintained compile-validated snippet is [`samples/AppHostSnippets/BunnyStorageAppHostSnippets.cs`](../samples/AppHostSnippets/BunnyStorageAppHostSnippets.cs).

## Deploy

```powershell
$env:Parameters__bunny_api_key = $env:BUNNY_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

The example uses a literal zone name. When using `BunnyStorageValue.FromParameter(...)`, also set the matching `Parameters__...` environment variable.

Repeated deployments should use the same configured zone name and options.

## Local Run

```powershell
aspire start --non-interactive --isolated
```

Local execution uses Azurite. Bunny credentials are resolved only by `aspire deploy`.
