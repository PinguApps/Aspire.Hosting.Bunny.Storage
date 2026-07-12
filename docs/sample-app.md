# Manual Sample Application

[`samples/BunnyStorageManual/`](../samples/BunnyStorageManual/) contains:

- a C# AppHost using Azurite locally and Bunny during deployment
- a minimal ASP.NET Core server application
- upload, existence, read, and public image URL examples
- application code that depends only on `IObjectStorage`

Build it from the repository root:

```powershell
dotnet build samples/BunnyStorageManual/AppHost/BunnyStorageManual.AppHost.csproj -c Release
```

Run locally from the AppHost directory:

```powershell
aspire start --non-interactive --isolated
```

The sample is compiled in CI. It does not delete Bunny resources during deployment or shutdown.
