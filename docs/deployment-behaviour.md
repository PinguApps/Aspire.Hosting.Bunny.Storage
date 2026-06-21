# Deployment Behaviour

`PublishToBunny` attaches deployment metadata and a provisioning pipeline step to an `AzureBlobStorageContainerResource`.

During `dotnet run` / Aspire run, `WithObjectStorage` injects:

```text
ObjectStorage__media__Provider=AzureBlob
```

During deploy/publish, it injects:

```text
ObjectStorage__media__Provider=Bunny
```

No fake Azure Blob connection string is emitted for Bunny.

Ownership modes:

- `CreateOnly`: fail if the Bunny storage zone already exists.
- `ExistingOnly`: fail if it does not exist.
- `CreateOrAdopt`: create if missing, otherwise adopt.

Remote identity is persisted through Aspire deployment state to avoid silently adopting the wrong renamed resource.
