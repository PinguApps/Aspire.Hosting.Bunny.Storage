# Overview

`PinguApps.Aspire.Hosting.Bunny.Storage` keeps Aspire's built-in Azure Blob container as the local resource of record while opting that container into Bunny Storage during deployment.

The integration is intentionally narrow:

- Bunny Storage plus an optional Bunny Pull Zone.
- Deploy-time provisioning only.
- Azurite remains the local development store.
- The explicit Bunny storage-zone name is the remote identity.
- Application code consumes `IObjectStorage` or `IObjectStorageProvider`.
- The package never auto-deletes Bunny resources.
- Application-facing outputs never contain the Bunny account API key.

## Product Contract

C# begins with normal Aspire Azure Storage:

```csharp
var media = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobContainer("media", "media")
    .PublishToBunny("myapp-media", bunnyApiKey, configure: options =>
    {
        options.CreatePullZone = true;
        options.PullZoneName = "myapp-media";
    });
```

TypeScript uses the generated `publishToBunny` method from the same NuGet package.

Consumers use `WithReference(media)` followed by `WithObjectStorage(media)`. During local execution this injects Azure Blob configuration. During deployment it removes the Azure connection reference and injects Bunny configuration from deploy outputs.

## Local Versus Deploy

`aspire start` uses Azurite and does not call Bunny. `aspire deploy` resolves the management parameters, creates or adopts the named storage zone, validates supported immutable settings, optionally creates or adopts a Pull Zone, and populates object-storage outputs.

`aspire publish` is not a substitute for deploy. Bunny outputs are created by the deployment pipeline, so application resources using `WithObjectStorage` deliberately fail manifest-only publishing with an actionable message.
