# Install

## C# AppHost

```powershell
dotnet add package PinguApps.Aspire.Hosting.Bunny.Storage
```

Import the hosting namespace:

```csharp
using Aspire.Hosting.Bunny.Storage;
```

Server-side applications that consume object storage also install:

```powershell
dotnet add package PinguApps.ObjectStorage
```

Do not install the runtime package in browser or Blazor WebAssembly projects because Bunny write credentials must remain server-side.

## TypeScript AppHost

TypeScript AppHosts consume the hosting integration through NuGet and Aspire's generated module, not through a separate npm package.

```json
{
  "packages": {
    "Aspire.Hosting.Azure.Storage": "13.4.6",
    "PinguApps.Aspire.Hosting.Bunny.Storage": "<package version>"
  }
}
```

Then generate the TypeScript surface:

```powershell
aspire restore --non-interactive
```

For this repository checkout, replace the version with the local project path:

```json
"PinguApps.Aspire.Hosting.Bunny.Storage": "../../src/Aspire.Hosting.Bunny.Storage/Aspire.Hosting.Bunny.Storage.csproj"
```

## Required Parameters

| Parameter | Secret | Purpose |
| --- | --- | --- |
| Storage-zone name | No | Stable Bunny storage-zone identity. It may be a C# literal or an Aspire parameter. TypeScript uses a parameter. |
| Bunny account API key | Yes | Infrastructure-only key used by the deploy pipeline. |

For a TypeScript AppHost using the names from the maintained sample:

```powershell
$env:Parameters__bunny_storage_zone_name = "myapp-media"
$env:Parameters__bunny_api_key = $env:BUNNY_API_KEY
```
