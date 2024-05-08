using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Build.Locator;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

public class MsBuildInitializer
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
    /// use MSBuildLocator to find MSBuild instances, return the first (highest version found).
    /// </summary>
    /// <returns></returns>
    private string RegisterMsbuild()
    {
        var msbuildPath = FindMSBuildPath();
        if (string.IsNullOrEmpty(msbuildPath))
        {
            _logger.LogMessage($"No msbuild path found!", LogMessageType.Error);
            return string.Empty;
        }

        var instance = MSBuildLocator.QueryVisualStudioInstances()
            .FirstOrDefault(i => string.Equals(msbuildPath, i.MSBuildPath, StringComparison.OrdinalIgnoreCase));
        if (instance is null)
        {
            _logger.LogMessage($"No msbuild find out at path '{msbuildPath}'!", LogMessageType.Error);
            return string.Empty;
        }

        if (!MSBuildLocator.IsRegistered)
        {
            // Must register instance rather than just path so everything gets set correctly for .NET SDK instances
            MSBuildLocator.RegisterInstance(instance);

        }

        var resolver = new AssemblyDependencyResolver(msbuildPath);

        AssemblyLoadContext.Default.Resolving += ResolveAssembly;

        var version = instance.Version.ToString();
        return version;

        Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (context is null || assemblyName is null)
            {
                return null;
            }

            if (resolver.ResolveAssemblyToPath(assemblyName) is string path)
            {
                return context.LoadFromAssemblyPath(path);
            }

            return null;
        }
    }

    private string? FindMSBuildPath()
    {
        var msBuildInstances = FilterForBitness(MSBuildLocator.QueryVisualStudioInstances())
            .OrderByDescending(m => m.Version)
            .ToList();

        if (msBuildInstances.Count == 0)
        {
            return string.Empty;
        }
        else
        {
            return msBuildInstances.FirstOrDefault()?.MSBuildPath;
        }
    }

    private IEnumerable<VisualStudioInstance> FilterForBitness(IEnumerable<VisualStudioInstance> instances)
    {
        foreach (var instance in instances)
        {
            var is32bit = instance.MSBuildPath.Contains("x86", StringComparison.OrdinalIgnoreCase);

            if (System.Environment.Is64BitProcess == !is32bit)
            {
                yield return instance;
            }
        }
    }
}
