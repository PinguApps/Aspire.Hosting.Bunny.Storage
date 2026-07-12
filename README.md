# PinguApps.Aspire.Hosting.Bunny.Storage

[![PinguApps.Aspire.Hosting.Bunny.Storage version](https://img.shields.io/nuget/v/PinguApps.Aspire.Hosting.Bunny.Storage?style=for-the-badge&label=PinguApps.Aspire.Hosting.Bunny.Storage)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Bunny.Storage/) [![PinguApps.Aspire.Hosting.Bunny.Storage downloads](https://img.shields.io/nuget/dt/PinguApps.Aspire.Hosting.Bunny.Storage?style=for-the-badge&label=downloads)](https://www.nuget.org/packages/PinguApps.Aspire.Hosting.Bunny.Storage/)

`PinguApps.Aspire.Hosting.Bunny.Storage` lets an Aspire AppHost publish a normal Azure Blob container to Bunny Storage during `aspire deploy`.

- Hosting package: `PinguApps.Aspire.Hosting.Bunny.Storage`
- Runtime package: `PinguApps.ObjectStorage`
- Distribution: NuGet for C# and TypeScript AppHosts
- Tested Aspire baseline: `13.4.6`
- Local behaviour: Azure Blob Storage through Azurite
- Deploy behaviour: opt-in Bunny Storage create/adopt validation, with optional Pull Zone creation

## Install

AppHost:

```powershell
dotnet add package PinguApps.Aspire.Hosting.Bunny.Storage
```

Server-side application:

```powershell
dotnet add package PinguApps.ObjectStorage
```

## Minimal C# Example

```csharp
using Aspire.Hosting;
using Aspire.Hosting.Bunny.Storage;

var builder = DistributedApplication.CreateBuilder(args);
var bunnyApiKey = builder.AddParameter("bunny-api-key", secret: true);

var media = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobContainer("media", "media")
    .PublishToBunny("myapp-media", bunnyApiKey, configure: options =>
    {
        options.Region = BunnyStorageRegion.De;
        options.CreatePullZone = true;
        options.PullZoneName = "myapp-media";
    });

builder.AddProject<Projects.Web>("web")
    .WithReference(media)
    .WithObjectStorage(media);

builder.Build().Run();
```

Register the runtime abstraction in the server-side application:

```csharp
builder.Services.AddObjectStorage(builder.Configuration);
```

Application code consumes `IObjectStorage` or `IObjectStorageProvider` and stores object keys such as `uploads/avatar.png`, not absolute URLs.

## Minimal TypeScript AppHost Example

TypeScript AppHosts consume the same NuGet package through Aspire's generated module flow:

```json
{
  "packages": {
    "Aspire.Hosting.Azure.Storage": "13.4.6",
    "PinguApps.Aspire.Hosting.Bunny.Storage": "<package version>"
  }
}
```

```powershell
aspire restore --non-interactive
```

```ts
import {
  BunnyStorageOwnershipMode,
  BunnyStorageRegion,
  createBuilder,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();
const zoneName = await builder.addParameter("bunny-storage-zone-name");
const apiKey = await builder.addParameter("bunny-api-key", { secret: true });

const storage = await builder.addAzureStorage("storage").runAsEmulator();
let media = await storage.addBlobContainer("media", { blobContainerName: "media" });
media = await media.publishToBunny(zoneName, apiKey, {
  ownershipMode: BunnyStorageOwnershipMode.CreateOrAdopt,
  region: BunnyStorageRegion.De,
  createPullZone: true,
  pullZoneName: "myapp-media",
});

let web = await builder.addProject("web", "../Web/Web.csproj");
web = await web.withReference(media);
web = await web.withObjectStorage(media);

await (await builder.build()).run();
```

## Deploy Inputs

| Input | Secret | Purpose |
| --- | --- | --- |
| Storage-zone name | No | Explicit Bunny storage-zone name and stable remote identity. |
| `bunny-api-key` | Yes | Infrastructure-only Bunny account API key. |

For parameter-backed non-interactive deploys:

```powershell
$env:Parameters__bunny_storage_zone_name = "myapp-media"
$env:Parameters__bunny_api_key = $env:BUNNY_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

The Bunny account API key is never exposed to application resources. Deployed applications receive the storage-zone access key instead.

## Behaviour Summary

The built-in `AzureBlobStorageContainerResource` remains the local resource of record. `PublishToBunny(...)` or `publishToBunny(...)` adds deploy-time intent. `WithObjectStorage(...)` switches application configuration between local Azure Blob Storage and deployed Bunny Storage without inventing an Azure connection string for Bunny.

The package never deletes Bunny resources automatically. Repeated deployments validate cached remote identity, ownership mode, region, replication regions, and Pull Zone linkage before reusing resources.

## Documentation

- [Overview and product contract](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/overview.md)
- [Installation](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/install.md)
- [C# AppHost usage](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/getting-started-dotnet.md)
- [TypeScript AppHost usage](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/getting-started-typescript.md)
- [Configuration and ownership modes](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/configuration.md)
- [Deployment behaviour](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/deployment-behaviour.md)
- [Runtime abstraction](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/runtime-abstraction.md)
- [Outputs and security](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/outputs-and-security.md)
- [Samples and validation](https://github.com/PinguApps/Aspire.Hosting.Bunny.Storage/blob/main/docs/samples-and-demos.md)
