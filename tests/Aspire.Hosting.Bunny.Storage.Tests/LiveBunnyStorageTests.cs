using System.Text;
using Aspire.Hosting.Bunny.Storage.Deployment;
using Aspire.Hosting.Bunny.Storage.Management;
using PinguApps.Bunny.Storage;

namespace Aspire.Hosting.Bunny.Storage.Tests;

public sealed class LiveBunnyStorageTests
{
    [Fact]
    public void DisposableResourceNamesAreBoundedAndUnique()
    {
        string first = LiveBunnyStorageSession.CreateDisposableResourceName(new string('a', 60));
        string second = LiveBunnyStorageSession.CreateDisposableResourceName(new string('a', 60));

        Assert.Equal(50, first.Length);
        Assert.NotEqual(first, second);
    }

    [Fact]
    [Trait("Category", "live-bunny")]
    public async Task LiveDeploymentRedeploysAndSupportsRuntimeObjectOperations()
    {
        using LiveBunnyStorageSession session = new();
        Assert.SkipUnless(
            session.HasExplicitOptIn,
            "Live Bunny tests require RUN_LIVE_BUNNY_TESTS=true in addition to provider credentials.");
        Assert.SkipUnless(session.HasCredentials, "Live Bunny tests require BUNNY_API_KEY.");

        string resourceName = LiveBunnyStorageSession.CreateDisposableResourceName("aspire-bunny-live");
        session.RegisterStorageZoneDeletion(resourceName);
        session.RegisterPullZoneDeletion(resourceName);

        try
        {
            BunnyStorageDeploymentOptions options = new()
            {
                Region = BunnyStorageRegion.De,
                CreatePullZone = true,
                PullZoneName = resourceName,
                PublicBaseUrl = $"https://{resourceName}.b-cdn.net",
            };
            BunnyStorageResolvedDeployment deployment = new(
                resourceName,
                BunnyStorageOwnershipMode.CreateOrAdopt,
                new BunnyStorageManagementCredentials(session.ApiKey!),
                options);
            BunnyStorageManagementClient managementClient = session.CreateManagementClient();

            BunnyStorageCreateFlowResult first = await BunnyStorageDeploymentPipeline.ExecuteAsync(
                deployment,
                managementClient,
                cachedIdentity: null,
                TestContext.Current.CancellationToken);
            BunnyStorageCreateFlowResult second = await BunnyStorageDeploymentPipeline.ExecuteAsync(
                deployment,
                managementClient,
                first.RemoteIdentity,
                TestContext.Current.CancellationToken);

            Assert.True(first.Created);
            Assert.False(second.Created);
            Assert.Equal(first.StorageZone.Id, second.StorageZone.Id);
            Assert.Equal(first.PullZone?.Id, second.PullZone?.Id);

            using HttpClient runtimeClient = new();
            BunnyObjectStorage storage = new(
                runtimeClient,
                first.StorageZone.Name,
                first.StorageZone.Password,
                deployment.StorageEndpoint,
                deployment.Options.PublicBaseUrl!);
            string objectKey = $"integration-tests/{Guid.NewGuid():N}.txt";
            byte[] content = Encoding.UTF8.GetBytes("Bunny Storage live integration test.");

            await PutWithReadinessRetryAsync(
                storage,
                objectKey,
                content,
                "text/plain",
                TestContext.Current.CancellationToken);
            Assert.True(await storage.ExistsAsync(objectKey, TestContext.Current.CancellationToken));
            await using Stream downloaded = await storage.OpenReadAsync(objectKey, TestContext.Current.CancellationToken);
            using MemoryStream buffer = new();
            await downloaded.CopyToAsync(buffer, TestContext.Current.CancellationToken);
            Assert.Equal(content, buffer.ToArray());
            await storage.DeleteAsync(objectKey, TestContext.Current.CancellationToken);
            Assert.False(await storage.ExistsAsync(objectKey, TestContext.Current.CancellationToken));
        }
        finally
        {
            await session.CleanupAsync();
        }
    }

    private static async Task PutWithReadinessRetryAsync(
        IObjectStorage storage,
        string objectKey,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        TimeSpan[] retryDelays =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
        ];

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using MemoryStream upload = new(content);
                await storage.PutAsync(objectKey, upload, contentType, cancellationToken);
                return;
            }
            catch (HttpRequestException exception)
                when (exception.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.NotFound
                    && attempt < retryDelays.Length)
            {
                await Task.Delay(retryDelays[attempt], cancellationToken);
            }
        }
    }
}
