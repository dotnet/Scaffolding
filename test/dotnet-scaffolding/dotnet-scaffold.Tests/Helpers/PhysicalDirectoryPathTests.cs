// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

public class PhysicalDirectoryPathTests
{
    [Fact]
    public void ResolveDirectoryPath_NormalizesRelativePaths()
    {
        var path = FileSystem.ResolveDirectoryPath(".");
        Assert.True(Path.IsPathFullyQualified(path));
        Assert.True(Directory.Exists(path));
        Assert.Equal(path, FileSystem.ResolveDirectoryPath(path));
    }

    [UnixFact]
    public void ResolveDirectoryPath_ResolvesLinksInParentDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(PhysicalDirectoryPathTests), Guid.NewGuid().ToString("N"));
        var actual = Path.Combine(root, "actual");
        var child = Path.Combine(actual, "child");
        Directory.CreateDirectory(child);
        var alias = Path.Combine(root, "alias");
        try
        {
            Directory.CreateSymbolicLink(alias, actual);
            Assert.Equal(FileSystem.ResolveDirectoryPath(child),
                FileSystem.ResolveDirectoryPath(Path.Combine(alias, "child")));
        }
        finally
        {
            if (Directory.Exists(alias))
            {
                Directory.Delete(alias);
            }
            Directory.Delete(root, recursive: true);
        }
    }
}
