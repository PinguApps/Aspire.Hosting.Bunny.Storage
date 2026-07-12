using Aspire.Hosting;

namespace Projects;

internal sealed class Web : IProjectMetadata
{
    public string ProjectPath => "../Web/Web.csproj";

    public LaunchSettings LaunchSettings { get; } = new();

    public bool SuppressBuild => true;

    public bool IsFileBasedApp => false;
}
