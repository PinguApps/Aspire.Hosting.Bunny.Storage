namespace Aspire.Hosting.Bunny.Storage.Management;

public enum BunnyStorageProviderFailureKind
{
    Authentication,
    NotFound,
    Conflict,
    Validation,
    Unexpected,
}
