#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Bunny.Storage;
using Aspire.Hosting.Bunny.Storage.Deployment;
using Aspire.Hosting.Bunny.Storage.Management;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    public void OptionsValidationRejectsSydAsPrimaryRegion()
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
                options.Region = BunnyStorageRegion.Syd;
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
    public async Task WithObjectStorageRejectsManifestPublishForBunnyOutputs()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<ParameterResource> apiKey = app.AddParameter("bunny-api-key", secret: true);
        IResourceBuilder<AzureBlobStorageContainerResource> media = app.AddAzureStorage("storage")
            .RunAsEmulator()
            .AddBlobContainer("media", "media")
            .PublishToBunny("my-zone", apiKey, configure: options => options.PublicBaseUrl = "https://my-zone.b-cdn.net");
        IResourceBuilder<ContainerResource> web = app.AddContainer("web", "example/web");

        web.WithObjectStorage(media);

        ManifestPublishingCallbackAnnotation annotation = web.Resource.Annotations
            .OfType<ManifestPublishingCallbackAnnotation>()
            .Single();
        Assert.NotNull(annotation.Callback);
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);
        ManifestPublishingContext context = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish, "manifest"),
            "manifest.json",
            writer,
            CancellationToken.None);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => annotation.Callback(context));

        Assert.Contains("aspire deploy", exception.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task CreateFlowRetriesWhenStorageZoneNameIsBeingDeleted()
    {
        FakeBunnyStorageManagementClient client = new()
        {
            StorageZoneCreateFailuresBeforeSuccess = 2,
        };
        List<TimeSpan> delays = [];
        BunnyStorageResolvedDeployment deployment = CreateDeployment();

        BunnyStorageCreateFlowResult result = await new BunnyStorageCreateFlow(
                client,
                (delay, _) =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                },
                [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)])
            .ExecuteAsync(
                deployment,
                new BunnyStorageOwnershipResolutionResult(BunnyStorageOwnershipResolutionAction.Create, ExistingZone: null),
                CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], delays);
        Assert.Equal(3, client.Interactions.Count(interaction => interaction == "POST /storagezone"));
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

    [Theory]
    [InlineData(false, false, "disabled")]
    [InlineData(true, true, "suspended")]
    public async Task PullZoneAdoptionFailsWhenExistingZoneCannotServeTraffic(bool enabled, bool suspended, string expectedMessage)
    {
        FakeBunnyStorageManagementClient client = new();
        client.PullZones.Add(new BunnyPullZoneDetails
        {
            Id = 10,
            Name = "my-zone",
            StorageZoneId = 123,
            OriginUrl = "https://storage.bunnycdn.com/my-zone/",
            Enabled = enabled,
            Suspended = suspended,
        });
        BunnyStorageResolvedDeployment deployment = CreateDeployment(createPullZone: true);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task CreateOrAdoptDetectsReplicationRegionDrift()
    {
        FakeBunnyStorageManagementClient client = new();
        client.StorageZones.Add(new BunnyStorageZoneDetails
        {
            Id = 1,
            Name = "my-zone",
            Password = "password",
            Region = "DE",
            ReplicationRegions = ["SG"],
        });
        BunnyStorageResolvedDeployment deployment = CreateDeployment();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity: null, CancellationToken.None));

        Assert.Contains("replication regions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CachedIdentityMustStillExist()
    {
        FakeBunnyStorageManagementClient client = new();
        client.StorageZones.Add(new BunnyStorageZoneDetails { Id = 2, Name = "my-zone", Password = "password", Region = "DE" });
        BunnyStorageResolvedDeployment deployment = CreateDeployment();
        BunnyStorageRemoteIdentityState cachedIdentity = new("my-zone", "1");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BunnyStorageDeploymentPipeline.ExecuteAsync(deployment, client, cachedIdentity, CancellationToken.None));

        Assert.Contains("no longer exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingCachedIdentityMessageIncludesDeploymentStateSection()
    {
        FakeBunnyStorageManagementClient client = new();
        BunnyStorageRemoteIdentityState cachedIdentity = new("my-zone", "1");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new BunnyStorageRemoteIdentityResolver(client).ResolveAsync(
                "my-zone",
                cachedIdentity,
                "Aspire.Hosting.Bunny.Storage.RemoteIdentity.media",
                CancellationToken.None));

        Assert.Contains("Aspire.Hosting.Bunny.Storage.RemoteIdentity.media", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClearCacheIgnoresSavedRemoteIdentity()
    {
        FakeDeploymentStateManager stateManager = new();
        BunnyStorageRemoteIdentityDeploymentStateStore store = new(stateManager);
        await store.SaveAsync("media", new BunnyStorageRemoteIdentityState("my-zone", "1"), CancellationToken.None);

        BunnyStorageRemoteIdentityState? cachedIdentity = await BunnyStorageDeploymentPipeline.LoadCachedIdentityAsync(
            store,
            "media",
            clearCache: true,
            CancellationToken.None);

        Assert.Null(cachedIdentity);
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
    public void OutputsRejectMissingStoragePassword()
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
        BunnyStorageZoneDetails zone = new()
        {
            Id = 123,
            Name = "my-zone",
            Password = "",
            Region = "DE",
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            outputs.Populate(zone, pullZone: null, storageEndpoint: "https://storage.bunnycdn.com", publicBaseUrl: "https://my-zone.b-cdn.net"));

        Assert.Contains("password", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task ManagementClientClassifiesDeletingStorageZoneName()
    {
        ResponseHandler handler = new(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"ErrorKey":"storagezone.name_taken","Field":"Name","Message":"The storage zone is currently being deleted."}"""),
        });
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://api.bunny.net/"),
        };
        BunnyStorageManagementClient client = new(httpClient, new BunnyStorageManagementCredentials("account-api-key"));

        BunnyStorageProviderException exception = await Assert.ThrowsAsync<BunnyStorageProviderException>(() =>
            client.CreateStorageZoneAsync("my-zone", "DE", [], CancellationToken.None));

        Assert.Equal(BunnyStorageProviderFailureKind.StorageZoneBeingDeleted, exception.FailureKind);
        Assert.Contains("\"Name\":\"my-zone\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"Region\":\"DE\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"ReplicationRegions\":[]", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementClientUsesListedPullZoneWhenCreateResponseIsEmpty()
    {
        QueueResponseHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.Created),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [{"Id":456,"Name":"my-zone","OriginUrl":"https://storage.bunnycdn.com/my-zone/","StorageZoneId":123}]
                    """),
            });
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://api.bunny.net/"),
        };
        BunnyStorageManagementClient client = new(httpClient, new BunnyStorageManagementCredentials("account-api-key"));

        BunnyPullZoneDetails pullZone = await client.CreatePullZoneAsync(
            "my-zone",
            "https://storage.bunnycdn.com/my-zone/",
            123,
            CancellationToken.None);

        Assert.Equal(456, pullZone.Id);
        Assert.All(handler.RequestPaths, path => Assert.EndsWith("/pullzone", path, StringComparison.Ordinal));
        Assert.Equal(2, handler.RequestPaths.Count);
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

        public int StorageZoneCreateFailuresBeforeSuccess { get; set; }

        public Task<IReadOnlyList<BunnyStorageZoneDetails>> ListStorageZonesAsync(CancellationToken cancellationToken)
        {
            Interactions.Add("GET /storagezone");
            return Task.FromResult<IReadOnlyList<BunnyStorageZoneDetails>>(StorageZones);
        }

        public Task<BunnyStorageZoneDetails> CreateStorageZoneAsync(string name, string region, IReadOnlyList<string> replicationRegions, CancellationToken cancellationToken)
        {
            Interactions.Add("POST /storagezone");
            if (StorageZoneCreateFailuresBeforeSuccess > 0)
            {
                StorageZoneCreateFailuresBeforeSuccess--;
                throw new BunnyStorageProviderException(
                    BunnyStorageProviderFailureKind.StorageZoneBeingDeleted,
                    "Bunny Storage zone name is still being deleted.");
            }

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

    private sealed class ResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
    }

    private sealed class QueueResponseHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<string> RequestPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestPaths.Add(request.RequestUri?.ToString() ?? "");
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class FakeDeploymentStateManager : IDeploymentStateManager
    {
        private readonly Dictionary<string, DeploymentStateSection> _sections = [];

        public string? StateFilePath => null;

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken)
        {
            if (!_sections.TryGetValue(sectionName, out DeploymentStateSection? section))
            {
                section = new DeploymentStateSection(sectionName, new JsonObject(), version: 0);
                _sections[sectionName] = section;
            }

            return Task.FromResult(section);
        }

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken)
        {
            _sections[section.SectionName] = section;
            return Task.CompletedTask;
        }

        public Task DeleteSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken)
        {
            _sections.Remove(section.SectionName);
            return Task.CompletedTask;
        }

        public Task ClearAllStateAsync(CancellationToken cancellationToken)
        {
            _sections.Clear();
            return Task.CompletedTask;
        }
    }
}
