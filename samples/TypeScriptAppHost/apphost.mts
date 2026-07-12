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
  createPullZone: true,
  pullZoneName: "myapp-media",
  publicBaseUrl: "https://myapp-media.b-cdn.net",
});

let web = await builder.addProject("web", "../BunnyStorageManual/Web/BunnyStorageManual.Web.csproj");
web = await web.withReference(media);
web = await web.withObjectStorage(media);

const app = await builder.build();
await app.run();
