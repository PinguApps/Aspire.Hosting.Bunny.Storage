namespace Aspire.Hosting.Bunny.Storage.Management;

public sealed class BunnyStorageProviderException : Exception
{
    public BunnyStorageProviderException()
    {
    }

    public BunnyStorageProviderException(string message)
        : base(message)
    {
    }

    public BunnyStorageProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BunnyStorageProviderException(BunnyStorageProviderFailureKind failureKind, string message)
        : base(message)
    {
        FailureKind = failureKind;
    }

    public BunnyStorageProviderFailureKind FailureKind { get; }
}
