namespace PinguApps.ObjectStorage;

internal static class ObjectStorageKey
{
    public static string Normalize(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (Uri.TryCreate(key, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Object storage keys must not be absolute URLs.", nameof(key));
        }

        string normalized = key.Replace('\\', '/').TrimStart('/');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Object storage key must not be empty.", nameof(key));
        }

        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment == ".."))
        {
            throw new ArgumentException("Object storage keys must not contain '..' path segments.", nameof(key));
        }

        return string.Join('/', segments);
    }

    public static string AppendEscaped(Uri publicBaseUrl, string key)
    {
        string normalized = Normalize(key);
        string baseUrl = publicBaseUrl.ToString().TrimEnd('/');
        string escapedKey = string.Join('/', normalized.Split('/').Select(Uri.EscapeDataString));
        return $"{baseUrl}/{escapedKey}";
    }
}
