# TypeScript AppHost Usage

TypeScript AppHosts use Aspire's generated module surface from `PinguApps.Aspire.Hosting.Bunny.Storage`.

Add the packages to `aspire.config.json`:

```json
{
  "packages": {
    "Aspire.Hosting.Azure.Storage": "13.4.6",
    "PinguApps.Aspire.Hosting.Bunny.Storage": "<package version>"
  }
}
```

Then generate the module:

```powershell
aspire restore --non-interactive
```

Authored `apphost.mts`:

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
  replicationRegions: [BunnyStorageRegion.Ny],
  createPullZone: true,
  pullZoneName: "myapp-media",
});

let web = await builder.addProject("web", "../Web/Web.csproj");
web = await web.withReference(media);
web = await web.withObjectStorage(media);

await (await builder.build()).run();
```

`withObjectStorage` is exported for project resources and should follow `withReference(media)`. The application project installs `PinguApps.Bunny.Storage` and registers `AddObjectStorage` exactly like a C# AppHost consumer.

The maintained demo is [`samples/TypeScriptAppHost/`](../samples/TypeScriptAppHost/). The NuGet-backed CI fixture is [`tests/Aspire.Hosting.Bunny.Storage.Tests/Fixtures/TypeScriptAppHost/`](../tests/Aspire.Hosting.Bunny.Storage.Tests/Fixtures/TypeScriptAppHost/).

## Validate

```powershell
npm install --no-audit --no-fund
aspire restore --non-interactive
npm run typecheck
aspire deploy --non-interactive --list-steps
```

Do not edit `.aspire/modules`; Aspire regenerates it.

## Deploy

```powershell
$env:Parameters__bunny_storage_zone_name = "myapp-media"
$env:Parameters__bunny_api_key = $env:BUNNY_API_KEY
aspire deploy --non-interactive --pipeline-log-level debug
```

The TypeScript method accepts parameter builders plus an optional DTO object. Generated enum members are PascalCase, such as `BunnyStorageRegion.De`.

## Outputs

```ts
const outputs = await media.getBunnyStorageOutputs();
const zoneNameOutput = await outputs.storageZoneName();
const endpoint = await outputs.storageEndpoint();
const accessKey = await outputs.accessKey();
const publicBaseUrl = await outputs.publicBaseUrl();
```

`accessKey` is secret. The Bunny account API key is not an output.
