// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Internal.Services;

internal interface IAppSettings
{
    //used for MSBuildWorkspace.Create()
    IDictionary<string, string> GlobalProperties { get; }
    object? GetSettings(string sectionName);
    void AddSettings(string sectionName, object settings);
}
