// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Steps;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Core.Scaffolders;
using System.Collections.Generic;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Scaffolders;

internal class CachingScaffolder : Scaffolder
{
    public CachingScaffolder(string type, string appHostProject, string project, bool prerelease, ILogger logger, IFileSystem fileSystem, IEnvironmentService environmentService)
    {
        if (!CachingPackagesDict.TryGetValue(type, out string? projectPackageName) ||
            !CachingConfigFileDict.TryGetValue(type, out string? configFileName))
        {
            throw new InvalidOperationException("Invalid type for caching scaffolder");
        }

        _steps.Add(new AddPackageReferenceStep(appHostProject, AppHostRedisPackageName, prerelease, logger, fileSystem));
        _steps.Add(new AddPackageReferenceStep(project, projectPackageName, prerelease, logger, fileSystem));
        _steps.Add(new CodeModificationStep(appHostProject, project, ConfigFile_RedisAppHost, logger, environmentService, modifyConfig: true));
        _steps.Add(new CodeModificationStep(appHostProject, project, configFileName, logger, environmentService, modifyConfig: false));
    }

    internal static readonly string Type_RedisWithOutputCaching = "redis-with-output-caching";
    internal static readonly string Type_Redis = "redis";
    internal static readonly string ConfigFile_RedisAppHost = "redis-apphost.json";
    internal static readonly string ConfigFile_RedisWebApp = "redis-webapp.json";
    internal static readonly string ConfigFile_RedisWithOutputCachingWebApp = "redis-webapp-oc.json";

    internal const string AppHostRedisPackageName = "Aspire.Hosting.Redis";
    internal const string WebAppRedisPackageName = "Aspire.StackExchange.Redis";
    internal const string WebAppRedisOutputCachingPackageName = "Aspire.StackExchange.Redis.OutputCaching";
    internal static readonly Dictionary<string, string> CachingPackagesDict = new()
    {
        { Type_Redis, WebAppRedisPackageName },
        { Type_RedisWithOutputCaching, WebAppRedisOutputCachingPackageName }
    };
    internal static readonly Dictionary<string, string> CachingConfigFileDict = new()
    {
        { Type_Redis, ConfigFile_RedisWebApp },
        { Type_RedisWithOutputCaching, ConfigFile_RedisWithOutputCachingWebApp }
    };
}
