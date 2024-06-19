using Microsoft.Build.Locator;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

internal class MsBuildInitializer
{
    private readonly ILogger _logger;
    public MsBuildInitializer(ILogger logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        RegisterMsbuild();
    }

    /// <summary>
    ///use MsBuildLocator.RegisterDefaults() to register the newest VS .NET SDK and MSBuild.
    /// </summary>
    /// <returns></returns>
    private void RegisterMsbuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
