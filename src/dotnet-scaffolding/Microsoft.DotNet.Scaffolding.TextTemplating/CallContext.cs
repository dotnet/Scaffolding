// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Utility class to provide a replacement for CallContext.LogicalGetData in generated .cs templates.
/// </summary>
internal sealed class CallContext
{
    /// <summary>
    /// Always returns null. Used as a stub for generated templates.
    /// </summary>
    /// <param name="name">The name of the data slot (unused).</param>
    /// <returns>Always null.</returns>
    public static object? LogicalGetData(string name)
        => null;
}
