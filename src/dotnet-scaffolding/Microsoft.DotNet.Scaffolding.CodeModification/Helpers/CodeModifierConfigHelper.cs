// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;

namespace Microsoft.DotNet.Scaffolding.CodeModification.Helpers;

internal static class CodeModifierConfigHelper
{
    public static CodeModifierConfig? GetCodeModifierConfig(string configPath)
    {
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            string jsonText = File.ReadAllText(configPath);
            return GetCodeModifierConfigFromJson(jsonText, Path.GetDirectoryName(Path.GetFullPath(configPath)));
        }

        return null;
    }

    public static CodeModifierConfig? GetCodeModifierConfigFromJson(string jsonText, string? configDirectory = null)
    {
        CodeModifierConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
        }
        catch (JsonException)
        {
            return null;
        }

        if (config is not null)
        {
            foreach (var snippet in config.GetCodeSnippets())
            {
                if (snippet.FileBlock is null)
                {
                    continue;
                }

                if (snippet.Block is not null || snippet.MultiLineBlock is not null)
                {
                    throw new InvalidDataException($"FileBlock '{snippet.FileBlock}' cannot be combined with Block or MultiLineBlock.");
                }

                if (configDirectory is null)
                {
                    throw new InvalidDataException("FileBlock requires a file-based configuration supplied through CodeModifierConfigPath.");
                }

                var relativePath = snippet.FileBlock.Replace('\\', Path.DirectorySeparatorChar);
                if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                {
                    throw new InvalidDataException($"FileBlock '{snippet.FileBlock}' must be a non-empty relative path.");
                }

                var filePath = Path.Combine(configDirectory, relativePath);
                snippet.Block = string.Join(Environment.NewLine, File.ReadAllLines(filePath));
            }
        }

        return config;
    }
}
