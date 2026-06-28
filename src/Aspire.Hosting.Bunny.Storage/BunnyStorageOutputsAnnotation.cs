using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Bunny.Storage;

internal sealed class BunnyStorageOutputsAnnotation : IResourceAnnotation
{
    public BunnyStorageOutputsAnnotation(BunnyStorageOutputs outputs) => Outputs = outputs;

    public BunnyStorageOutputs Outputs { get; }
}
