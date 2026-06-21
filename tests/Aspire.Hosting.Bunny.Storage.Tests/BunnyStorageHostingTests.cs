#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Bunny.Storage;
using Aspire.Hosting.Bunny.Storage.Deployment;
using Aspire.Hosting.Bunny.Storage.Management;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Bunny.Storage.Tests;

public sealed class BunnyStorageHostingTests
{
    [Fact]
    public void PublishToBunnyAttachesAnnotationOutputsAndPipelineStep()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<ParameterResource> apiKey = app.AddParameter("bunny-api-key", secret: true);

        IResourceBuilder<AzureBlobStorageContainerResource> media = app.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media")
            .PublishToBunny("my-zone", apiKey, configure: options => options.PublicBaseUrl = "https://my-zone.b-cdn.net");

        Assert.NotNull(media.Resource.Annotations.OfType<BunnyStorageDeploymentAnnotation>().SingleOrDefault());
        Assert.NotNull(media.Resource.Annotations.OfType<BunnyStorageOutputsAnnotation>().SingleOrDefault());
        Assert.Contains(media.Resource.Annotations, annotation => annotation is PipelineStepAnnotation);
    }

    [Fact]
    public void DuplicatePublishToBunnyReplacesOldPipelineStep()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<ParameterResource> apiKey = app.AddParameter("bunny-api-key", secret: true);
        IResourceBuilder<AzureBlobStorageContainerResource> media = app.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media");

        media.PublishToBunny("first-zone", apiKey, configure: options => options.PublicBaseUrl = "https://first-zone.b-cdn.net");
        media.PublishToBunny("second-zone", apiKey, configure: options => options.PublicBaseUrl = "https://second-zone.b-cdn.net");

        Assert.Single(media.Resource.Annotations.OfType<BunnyStorageDeploymentAnnotation>());
        Assert.Single(media.Resource.Annotations.OfType<PipelineStepAnnotation>());
        Assert.Equal("second-zone", media.Resource.GetBunnyStorageDeploymentState()!.StorageZoneName.LiteralValue);
    }

    [Fact]
    public void OptionsValidationRejectsPrimaryRegionAsReplicationRegion()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<ParameterResource> apiKey = app.AddParameter("bunny-api-key", secret: true);
        IResourceBuilder<AzureBlobStorageContainerResource> media = app.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            media.PublishToBunny("zone", apiKey, configure: options =>
            {
                options.PublicBaseUrl = "https://zone.b-cdn.net";
                options.SetReplicationRegions(BunnyStorageRegion.De);
            }));
        Assert.Contains("primary region", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OptionsValidationRejectsMissingPublicReadConfiguration()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<ParameterResource> apiKey = app.AddParameter("bunny-api-key", secret: true);
        IResourceBuilder<AzureBlobStorageContainerResource> media = app.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            media.PublishToBunny("zone", apiKey));

        Assert.Contains("PublicBaseUrl", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CreatePullZone", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateFlowCreatesMissingStorageZoneAndPullZone()
    {
        FakeBunnyStorageManagementClient client = new();
        BunnyStorageResolvedDeployment deployment = CreateDeployment(createPullZone: true);

        BunnyStorageCreateFlowResult result = await BunnyStorageDeploymentPipeline.ExecuteAsync(
            deployment,
            client,
            cachedIdentity: null,
            CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal("my-zone", result.StorageZone.Name);
        Assert.Equal("my-zone", result.PullZone!.Name);
        Assert.Contains(client.Interactions, interaction => interaction == "POST /storagezone");
        Assert.Contains(client.Interactions, interaction => interaction == "POST /pullzone");
    }

    [Fact]
    public async Task ExistingOnlyFailsIfMissing()
    {
        FakeBunnyStorageManagementClient client = new();
        BunnyStorageResolvedDeployment deployment = CreateDeployment(BunnyStorageOwnershipMode.ExistingOnly);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));
        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOnlyFailsIfExisting()
    {
        FakeBunnyStorageManagementClient client = new();
        client.StorageZones.Add(new BunnyStorageZoneDetails { Id = 1, Name = "my-zone", Password = "password", Region = "DE" });
        BunnyStorageResolvedDeployment deployment = CreateDeployment(BunnyStorageOwnershipMode.CreateOnly);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));
        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PullZoneAdoptionFailsWhenExistingZoneTargetsDifferentStorageZone()
    {
        FakeBunnyStorageManagementClient client = new();
        client.PullZones.Add(new BunnyPullZoneDetails
        {
            Id = 10,
            Name = "my-zone",
            StorageZoneId = 999,
            OriginUrl = "https://storage.bunnycdn.com/other-zone/",
        });
        BunnyStorageResolvedDeployment deployment = CreateDeployment(createPullZone: true);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));

        Assert.Contains("linked to storage zone", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrAdoptAdoptsExistingAndDetectsImmutableDrift()
    {
        FakeBunnyStorageManagementClient client = new();
        client.StorageZones.Add(new BunnyStorageZoneDetails { Id = 1, Name = "my-zone", Password = "password", Region = "NY" });
        BunnyStorageResolvedDeployment deployment = CreateDeployment();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));
        Assert.Contains("immutable drift", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImmutableDriftFailsBeforePullZoneWork()
    {
        FakeBunnyStorageManagementClient client = new();
        client.StorageZones.Add(new BunnyStorageZoneDetails { Id = 1, Name = "my-zone", Password = "password", Region = "NY" });
        BunnyStorageResolvedDeployment deployment = CreateDeployment(createPullZone: true);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));

        Assert.Contains("immutable drift", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GET /pullzone", client.Interactions);
        Assert.DoesNotContain("POST /pullzone", client.Interactions);
    }

    [Fact]
    public async Task AccessKeyOutputIsSecretAndOutputsPopulate()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<AzureBlobStorageContainerResource> media = app.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media");
        media.PublishToBunny(
            "zone",
            app.AddParameter("bunny-api-key", secret: true),
            configure: options => options.PublicBaseUrl = "https://my-zone.b-cdn.net");
        BunnyStorageOutputs outputs = media.GetBunnyStorageOutputs()!;
        BunnyStorageResolvedDeployment deployment = CreateDeployment();
        FakeBunnyStorageManagementClient client = new();

        BunnyStorageCreateFlowResult result = await BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, null, CancellationToken.None);
        outputs.Populate(result.StorageZone, result.PullZone, deployment.StorageEndpoint, BunnyStorageDeploymentPipeline.ResolvePublicBaseUrl(deployment, result.PullZone));

        Assert.True(BunnyStorageOutputs.IsSecret(BunnyStorageOutputNames.AccessKey));
        Assert.False(BunnyStorageOutputs.IsSecret(BunnyStorageOutputNames.PublicBaseUrl));
        Assert.Equal("password", await outputs.AccessKey.GetValueAsync(CancellationToken.None));
    }

    [Fact]
    public void ManagementClientRejectsMissingHttpClientBaseAddress()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new BunnyStorageManagementClient(new HttpClient(), new BunnyStorageManagementCredentials("account-api-key")));

        Assert.Contains("BaseAddress", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagementClientRejectsNullCredentials()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BunnyStorageManagementClient(new HttpClient { BaseAddress = new Uri("https://api.bunny.net/") }, credentials: null!));
    }

    private static BunnyStorageResolvedDeployment CreateDeployment(
        BunnyStorageOwnershipMode ownershipMode = BunnyStorageOwnershipMode.CreateOrAdopt,
        bool createPullZone = false)
    {
        BunnyStorageDeploymentOptions options = new()
        {
            Region = BunnyStorageRegion.De,
            CreatePullZone = createPullZone,
            PullZoneName = createPullZone ? "my-zone" : null,
            PublicBaseUrl = "https://my-zone.b-cdn.net",
        };
        options.SetReplicationRegions(BunnyStorageRegion.Ny);
        return new BunnyStorageResolvedDeployment(
            "my-zone",
            ownershipMode,
            new BunnyStorageManagementCredentials("account-api-key"),
            options);
    }

    private sealed class FakeBunnyStorageManagementClient : IBunnyStorageManagementClient
    {
        public List<string> Interactions { get; } = [];

        public List<BunnyStorageZoneDetails> StorageZones { get; } = [];

        public List<BunnyPullZoneDetails> PullZones { get; } = [];

        public Task<IReadOnlyList<BunnyStorageZoneDetails>> ListStorageZonesAsync(CancellationToken cancellationToken)
        {
            Interactions.Add("GET /storagezone");
            return Task.FromResult<IReadOnlyList<BunnyStorageZoneDetails>>(StorageZones);
        }

        public Task<BunnyStorageZoneDetails> CreateStorageZoneAsync(string name, string region, IReadOnlyList<string> replicationRegions, CancellationToken cancellationToken)
        {
            Interactions.Add("POST /storagezone");
            BunnyStorageZoneDetails zone = new()
            {
                Id = 123,
                Name = name,
                Password = "password",
                Region = region,
                ReplicationRegions = [.. replicationRegions],
            };
            StorageZones.Add(zone);
            return Task.FromResult(zone);
        }

        public Task<IReadOnlyList<BunnyPullZoneDetails>> ListPullZonesAsync(CancellationToken cancellationToken)
        {
            Interactions.Add("GET /pullzone");
            return Task.FromResult<IReadOnlyList<BunnyPullZoneDetails>>(PullZones);
        }

        public Task<BunnyPullZoneDetails> CreatePullZoneAsync(string name, string originUrl, long storageZoneId, CancellationToken cancellationToken)
        {
            Interactions.Add("POST /pullzone");
            BunnyPullZoneDetails pullZone = new()
            {
                Id = 456,
                Name = name,
                OriginUrl = originUrl,
                StorageZoneId = storageZoneId,
            };
            PullZones.Add(pullZone);
            return Task.FromResult(pullZone);
        }
    }
}
