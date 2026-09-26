// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

internal static class GeneratedProjectBaseline
{
    public static Dictionary<string, string> ReadFiles(string directory) =>
        EnumerateFiles(directory).ToDictionary(
            path => Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/'),
            path => Path.GetExtension(path) is ".png" or ".ico"
                ? Convert.ToBase64String(File.ReadAllBytes(path))
                : File.ReadAllText(path).Replace("\r\n", "\n"),
            StringComparer.Ordinal);

    public static IEnumerable<string> EnumerateFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(directory, path).Split(Path.DirectorySeparatorChar)
                .Any(part => part is "bin" or "obj" or ".vs"))
            .Where(path => Path.GetFileName(path) != "AGENTS.md" &&
                !path.EndsWith(".db", StringComparison.Ordinal) &&
                !path.EndsWith(".db-shm", StringComparison.Ordinal) &&
                !path.EndsWith(".db-wal", StringComparison.Ordinal));

    public static void AssertMatches(
        IReadOnlyDictionary<string, string> input,
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual,
        IReadOnlyCollection<string> scaffoldedFiles)
    {
        var paths = input.Keys.Concat(scaffoldedFiles).ToHashSet(StringComparer.Ordinal);
        var differences = new List<string>();
        foreach (var path in paths.Except(actual.Keys).Order(StringComparer.Ordinal))
        {
            differences.Add($"Missing file: {path}");
        }

        foreach (var path in actual.Keys.Except(paths).Order(StringComparer.Ordinal))
        {
            differences.Add($"Unexpected file: {path}");
        }

        foreach (var path in expected.Keys.Except(input.Keys).Except(scaffoldedFiles).Order(StringComparer.Ordinal))
        {
            differences.Add($"Baseline file is not listed in scaffolded-files.txt: {path}");
        }

        foreach (var path in paths.Order(StringComparer.Ordinal))
        {
            var isScaffolded = scaffoldedFiles.Contains(path);
            var source = isScaffolded ? expected : input;
            if (!source.TryGetValue(path, out var expectedContent))
            {
                differences.Add($"Missing baseline file: {path}");
            }
            else if (actual.TryGetValue(path, out var actualContent) && expectedContent != actualContent)
            {
                var expectedLines = expectedContent.Split('\n');
                var actualLines = actualContent.Split('\n');
                var line = Enumerable.Range(0, Math.Max(expectedLines.Length, actualLines.Length))
                    .First(index => expectedLines.ElementAtOrDefault(index) != actualLines.ElementAtOrDefault(index));
                differences.Add($"{path}:{line + 1} ({(isScaffolded ? "baseline mismatch" : "input file must remain unchanged")})\n" +
                    $"  Expected: {expectedLines.ElementAtOrDefault(line) ?? "<end of file>"}\n" +
                    $"  Actual:   {actualLines.ElementAtOrDefault(line) ?? "<end of file>"}");
            }
        }

        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    public static void CopyProject(string source, string destination)
    {
        foreach (var path in EnumerateFiles(source))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target, overwrite: true);
        }
    }
}
