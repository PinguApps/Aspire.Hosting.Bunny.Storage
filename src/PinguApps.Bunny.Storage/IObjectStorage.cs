namespace PinguApps.Bunny.Storage;

/// <summary>Provider-neutral object storage operations used by application code.</summary>
public interface IObjectStorage
{
    /// <summary>Writes an object, replacing any existing object with the same key.</summary>
    public Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Opens an object for reading.</summary>
    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an object exists.</summary>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes an object if it exists.</summary>
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns a public read URL for an object key.</summary>
    public string GetPublicUrl(string key);
}
