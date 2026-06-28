namespace Aspire.Hosting.Bunny.Storage.Management;

public sealed class BunnyStorageProviderException : Exception
{
    public BunnyStorageProviderException(BunnyStorageProviderFailureKind failureKind, string message)
        : base(message)
    {
        FailureKind = failureKind;
    }

    public BunnyStorageProviderFailureKind FailureKind { get; }
}
