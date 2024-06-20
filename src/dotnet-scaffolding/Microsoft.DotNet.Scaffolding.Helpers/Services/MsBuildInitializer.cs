using Microsoft.Build.Locator;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

internal static class MsBuildInitializer
{

    /// <summary>
    ///use MsBuildLocator.RegisterDefaults() to register the newest VS .NET SDK and MSBuild.
    /// </summary>
    /// <returns></returns>
    internal static void RegisterMsbuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
