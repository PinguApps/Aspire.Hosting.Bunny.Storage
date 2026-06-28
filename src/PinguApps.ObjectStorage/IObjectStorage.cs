namespace PinguApps.ObjectStorage;

/// <summary>Provider-neutral object storage operations used by application code.</summary>
public interface IObjectStorage
{
    /// <summary>Writes an object, replacing any existing object with the same key.</summary>
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Opens an object for reading.</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an object exists.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes an object if it exists.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns a public read URL for an object key.</summary>
    string GetPublicUrl(string key);
}
