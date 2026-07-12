using System.Net;
using System.Runtime.ExceptionServices;
using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Tests;

internal sealed class LiveBunnyStorageSession : IDisposable
{
    private readonly Stack<Func<Task>> _cleanupActions = [];
    private readonly HttpClient _managementHttpClient = new()
    {
        BaseAddress = new Uri("https://api.bunny.net/"),
    };

    public LiveBunnyStorageSession()
    {
        LoadDotEnv();
        ApiKey = Environment.GetEnvironmentVariable("BUNNY_API_KEY");
    }

    public string? ApiKey { get; }

    public bool HasCredentials => !string.IsNullOrWhiteSpace(ApiKey);

    public bool HasExplicitOptIn => bool.TryParse(
        Environment.GetEnvironmentVariable("RUN_LIVE_BUNNY_TESTS"),
        out bool enabled) && enabled;

    public BunnyStorageManagementClient CreateManagementClient()
    {
        return new BunnyStorageManagementClient(
            _managementHttpClient,
            new BunnyStorageManagementCredentials(
                ApiKey ?? throw new InvalidOperationException("BUNNY_API_KEY is not configured.")));
    }

    public static string CreateDisposableResourceName(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        const int maxLength = 50;
        const int suffixLength = 12;
        string truncatedPrefix = prefix[..Math.Min(prefix.Length, maxLength - suffixLength - 1)];
        string suffix = $"{Guid.NewGuid():N}"[..suffixLength];
        return $"{truncatedPrefix}-{suffix}";
    }

    public void RegisterStorageZoneDeletion(string storageZoneName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageZoneName);
        _cleanupActions.Push(() => DeleteStorageZoneByNameAsync(storageZoneName));
    }

    public void RegisterPullZoneDeletion(string pullZoneName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pullZoneName);
        _cleanupActions.Push(() => DeletePullZoneByNameAsync(pullZoneName));
    }

    public async Task CleanupAsync()
    {
        List<Exception>? failures = null;
        while (_cleanupActions.TryPop(out Func<Task>? cleanup))
        {
            try
            {
                await cleanup().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is null)
        {
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        throw new AggregateException("One or more live Bunny cleanup actions failed.", failures);
    }

    public void Dispose()
    {
        _managementHttpClient.Dispose();
    }

    private async Task DeletePullZoneByNameAsync(string pullZoneName)
    {
        BunnyPullZoneDetails? pullZone = (await CreateManagementClient()
            .ListPullZonesAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .SingleOrDefault(candidate => string.Equals(candidate.Name, pullZoneName, StringComparison.OrdinalIgnoreCase));
        if (pullZone is null)
        {
            return;
        }

        await DeleteAsync($"pullzone/{pullZone.Id}").ConfigureAwait(false);
    }

    private async Task DeleteStorageZoneByNameAsync(string storageZoneName)
    {
        BunnyStorageZoneDetails? storageZone = (await CreateManagementClient()
            .ListStorageZonesAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .SingleOrDefault(candidate => string.Equals(candidate.Name, storageZoneName, StringComparison.OrdinalIgnoreCase));
        if (storageZone is null)
        {
            return;
        }

        await DeleteAsync($"storagezone/{storageZone.Id}").ConfigureAwait(false);
    }

    private async Task DeleteAsync(string path)
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, path);
        request.Headers.Add(
            "AccessKey",
            ApiKey ?? throw new InvalidOperationException("BUNNY_API_KEY is not configured for live-test cleanup."));
        using HttpResponseMessage response = await _managementHttpClient
            .SendAsync(request, CancellationToken.None)
            .ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private static void LoadDotEnv()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path))
            {
                LoadDotEnv(path);
                return;
            }

            directory = directory.Parent;
        }
    }

    private static void LoadDotEnv(string path)
    {
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            string name = line[..separator].Trim();
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            {
                continue;
            }

            string value = line[(separator + 1)..].Trim().Trim('"', '\'');
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
