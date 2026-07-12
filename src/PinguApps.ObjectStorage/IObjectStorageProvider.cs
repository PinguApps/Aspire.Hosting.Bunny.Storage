namespace PinguApps.ObjectStorage;

/// <summary>Resolves named object storage registrations.</summary>
public interface IObjectStorageProvider
{
    /// <summary>Returns the named storage registration, or throws if it is missing.</summary>
    public IObjectStorage GetRequiredStorage(string name);
}
