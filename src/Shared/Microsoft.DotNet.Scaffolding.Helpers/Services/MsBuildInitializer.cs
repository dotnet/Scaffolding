using Microsoft.Build.Locator;

using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

public class MsBuildInitializer
{
    private readonly ILogger _logger;

    public MsBuildInitializer(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        RegisterMsbuild();
    }

    private string RegisterMsbuild()
    {
        var msbuildPath = FindMSBuildPath();
        var instance = MSBuildLocator.QueryVisualStudioInstances()
            .FirstOrDefault(i => string.Equals(msbuildPath, i.MSBuildPath, StringComparison.OrdinalIgnoreCase));

        if (instance is null)
        {
            _logger.LogMessage($"No msbuild find out at path '{msbuildPath}'", LogMessageType.Error);
            return string.Empty;
        }

        // Must register instance rather than just path so everything gets set correctly for .NET SDK instances
        MSBuildLocator.RegisterInstance(instance);

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
                //_logger.LogDebug(string.Format(Strings.AssemblyLoadedFromPath, assemblyName.FullName, context.Name, path));
                return context.LoadFromAssemblyPath(path);
            }

            //_logger.LogDebug(string.Format(Strings.UnableToResolveAssembly, assemblyName.FullName));
            return null;
        }
    }

    private string FindMSBuildPath()
    {
        var msBuildInstances = FilterForBitness(MSBuildLocator.QueryVisualStudioInstances())
            .OrderByDescending(m => m.Version)
            .ToList();

        if (msBuildInstances.Count == 0)
        {
            //_logger.LogError(string.Format(Strings.NoMsbuildFound, ExpectedBitness));
            return string.Empty;
        }
        else
        {
            foreach (var instance in msBuildInstances)
            {
                //_logger.LogDebug(string.Format(Strings.FoundCandidateMsBuildInstance, instance.MSBuildPath));
            }

            var selected = msBuildInstances.First();

            return selected.MSBuildPath;
        }
    }

    private IEnumerable<VisualStudioInstance> FilterForBitness(IEnumerable<VisualStudioInstance> instances)
    {
        foreach (var instance in instances)
        {
            var is32bit = instance.MSBuildPath.Contains("x86", StringComparison.OrdinalIgnoreCase);

            if (Environment.Is64BitProcess == !is32bit)
            {
                yield return instance;
            }
            else
            {
                //_logger.LogMessage(string.Format(Strings.SkippingToolWithInconsistentBitnesss, instance.MSBuildPath, ExpectedBitness));
            }
        }
    }
}
