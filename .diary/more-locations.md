## Rolling state
- Goal: Support every documented Bunny Storage primary-region endpoint.
- Current plan: Complete; all nine regions are modelled, mapped, and tested.
- Open questions/risks: Bunny's create-storage-zone API reference still lists the former four primary codes despite its HTTP docs listing nine endpoints.
- Next actions: None.
- Key paths: `src/Aspire.Hosting.Bunny.Storage/BunnyStorageRegion.cs`, `src/Aspire.Hosting.Bunny.Storage/BunnyStorageRegionExtensions.cs`

## Session log
### 2026-07-13 01:15 +01:00 (feature/more-locations)
- Add London, Stockholm, São Paulo, and Johannesburg regions [api] (impact: med)
  - Why: Match all primary-region endpoints documented by Bunny and enable London deployments.
  - Change: Added provider-code and runtime-endpoint mappings plus exhaustive mapping coverage (files: `src/Aspire.Hosting.Bunny.Storage/BunnyStorageRegion.cs`, `src/Aspire.Hosting.Bunny.Storage/BunnyStorageRegionExtensions.cs`, `tests/Aspire.Hosting.Bunny.Storage.Tests/BunnyStorageHostingTests.cs` | cmds: `dotnet test Aspire.Hosting.Bunny.Storage.slnx --no-restore`, `./eng/Validate-TypeScriptAppHostPackage.ps1`)
  - Notes: Preserved numeric values of existing public enum members; 65 tests passed, one live test skipped, packed TypeScript validation passed.
