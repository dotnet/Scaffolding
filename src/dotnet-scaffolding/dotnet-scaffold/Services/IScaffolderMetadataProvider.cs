// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Provides metadata for available scaffolding components.
/// </summary>
internal interface IScaffolderMetadataProvider
{
    IReadOnlyList<ScaffolderComponent> GetComponents();
}
