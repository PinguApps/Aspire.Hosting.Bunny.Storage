# Getting Started

AppHost:

```csharp
using Aspire.Hosting.Bunny.Storage;

var bunnyApiKey = builder.AddParameter("bunny-api-key", secret: true);

var media = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobContainer("media", "media")
    .PublishToBunny("myapp-media", bunnyApiKey, configure: options =>
    {
        options.CreatePullZone = true;
        options.PullZoneName = "myapp-media";
        options.PublicBaseUrl = "https://myapp-media.b-cdn.net";
    });

builder.AddProject<Projects.Web>("web")
    .WithReference(media)
    .WithObjectStorage(media);
```

Web/API:

```csharp
builder.Services.AddObjectStorage(builder.Configuration);
```

Use `IObjectStorage`:

```csharp
await storage.PutAsync("images/example.png", stream, "image/png");
string url = storage.GetPublicUrl("images/example.png");
```
