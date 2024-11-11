// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

/* NOTE
 * to use MSBuildProjectService, we need to initialize the MsBuildInitializer
 * that is because MSBuildLocator.Register fails if Microsoft.Build assemblies are already pulled in
 * using MSBuildProjectService does that unfortunately so this initialization cannot happen in this service
*/
internal interface IMSBuildProjectService
{
    string? GetLowestTargetFramework(bool refresh = false);
    IEnumerable<string> GetProjectCapabilities(bool refresh = false);
}
