using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PinguApps.ObjectStorage;

/// <summary>Registers object storage from configuration.</summary>
public static class ObjectStorageServiceCollectionExtensions
{
    /// <summary>Registers all stores under the <c>ObjectStorage</c> configuration section.</summary>
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection root = configuration.GetSection("ObjectStorage");
        List<string> names = [.. root.GetChildren().Select(section => section.Key)];
        if (names.Count == 0)
        {
            throw new InvalidOperationException("No object storage registrations were found under configuration section 'ObjectStorage'.");
        }

        services.AddHttpClient();
        services.AddSingleton<IObjectStorageProvider>(serviceProvider =>
        {
            Dictionary<string, IObjectStorage> stores = new(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                stores[name] = CreateStorage(name, root.GetSection(name), serviceProvider);
            }

            return new ConfiguredObjectStorageProvider(stores);
        });

        if (names.Count == 1)
        {
            string onlyName = names[0];
            services.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IObjectStorageProvider>().GetRequiredStorage(onlyName));
        }

        return services;
    }

    /// <summary>Registers one named store as the bare <see cref="IObjectStorage"/> service.</summary>
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, string name, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpClient();
        services.AddSingleton<IObjectStorageProvider>(serviceProvider =>
        {
            IObjectStorage storage = CreateStorage(name, configuration.GetSection("ObjectStorage").GetSection(name), serviceProvider);
            return new ConfiguredObjectStorageProvider(new Dictionary<string, IObjectStorage>(StringComparer.OrdinalIgnoreCase)
            {
                [name] = storage,
            });
        });
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IObjectStorageProvider>().GetRequiredStorage(name));
        return services;
    }

    private static IObjectStorage CreateStorage(string name, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        ObjectStorageOptions options = configuration.Get<ObjectStorageOptions>()
            ?? throw new InvalidOperationException($"Object storage '{name}' is missing configuration.");

        if (string.Equals(options.Provider, nameof(ObjectStorageProviderKind.AzureBlob), StringComparison.OrdinalIgnoreCase))
        {
            Require(options.Azure.ConnectionString, name, "Azure:ConnectionString");
            Require(options.Azure.ContainerName, name, "Azure:ContainerName");
            return new AzureBlobObjectStorage(
                options.Azure.ConnectionString!,
                options.Azure.ContainerName!,
                options.PublicBaseUrl!);
        }

        if (string.Equals(options.Provider, nameof(ObjectStorageProviderKind.Bunny), StringComparison.OrdinalIgnoreCase))
        {
            Require(options.PublicBaseUrl, name, "PublicBaseUrl");
            Require(options.Bunny.StorageZoneName, name, "Bunny:StorageZoneName");
            Require(options.Bunny.AccessKey, name, "Bunny:AccessKey");
            Require(options.Bunny.Endpoint, name, "Bunny:Endpoint");
            IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return new BunnyObjectStorage(
                httpClientFactory.CreateClient("PinguApps.ObjectStorage.Bunny"),
                options.Bunny.StorageZoneName!,
                options.Bunny.AccessKey!,
                options.Bunny.Endpoint!,
                options.PublicBaseUrl!);
        }

        throw new InvalidOperationException(
            $"Object storage '{name}' has unknown provider '{options.Provider}'. Supported providers are AzureBlob and Bunny.");
    }

    private static void Require(string? value, string name, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Object storage '{name}' is missing required configuration 'ObjectStorage:{name}:{key}'.");
        }
    }

    private sealed class ConfiguredObjectStorageProvider : IObjectStorageProvider
    {
        private readonly IReadOnlyDictionary<string, IObjectStorage> _stores;

        public ConfiguredObjectStorageProvider(IReadOnlyDictionary<string, IObjectStorage> stores)
        {
            _stores = stores;
        }

        public IObjectStorage GetRequiredStorage(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (_stores.TryGetValue(name, out IObjectStorage? storage))
            {
                return storage;
            }

            throw new KeyNotFoundException($"Object storage '{name}' is not registered.");
        }
    }
}
