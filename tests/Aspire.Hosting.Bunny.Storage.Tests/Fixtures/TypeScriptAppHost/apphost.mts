import {
  BunnyStorageOwnershipMode,
  BunnyStorageRegion,
  createBuilder,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const storageZoneName = await builder.addParameter("bunny-storage-zone-name");
const apiKey = await builder.addParameter("bunny-api-key", { secret: true });

const storage = await builder.addAzureStorage("storage").runAsEmulator();
let media = await storage.addBlobContainer("media", { blobContainerName: "media" });
media = await media.publishToBunny(storageZoneName, apiKey, {
  ownershipMode: BunnyStorageOwnershipMode.CreateOrAdopt,
  region: BunnyStorageRegion.De,
  replicationRegions: [BunnyStorageRegion.Ny],
  createPullZone: false,
  publicBaseUrl: "https://cdn.example.com",
});

const outputs = await media.getBunnyStorageOutputs();
const outputReferences = [
  await outputs.storageZoneId(),
  await outputs.storageZoneName(),
  await outputs.storageEndpoint(),
  await outputs.accessKey(),
  await outputs.pullZoneId(),
  await outputs.pullZoneName(),
  await outputs.publicBaseUrl(),
];

let worker = await builder.addProject("worker", "Projects/Worker/Worker.csproj");
worker = await worker.withReference(media);
worker = await worker.withObjectStorage(media);
void outputReferences;

const app = await builder.build();
await app.run();
