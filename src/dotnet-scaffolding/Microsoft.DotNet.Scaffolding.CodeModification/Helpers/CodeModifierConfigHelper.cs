// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.CodeModification.Helpers;

internal static class CodeModifierConfigHelper
{
    public static CodeModifierConfig? GetCodeModifierConfig(string configName, Assembly? assembly = null)
    {
        assembly = assembly ?? Assembly.GetExecutingAssembly();
        string jsonText = ProjectModelHelper.GetManifestResource(assembly, shortResourceName: configName);
        return System.Text.Json.JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
    }

    public static CodeModifierConfig? GetCodeModifierConfig(string configPath)
    {
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            string jsonText = File.ReadAllText(configPath);
            return GetCodeModifierConfigFromJson(jsonText);
        }

        return null;
    }

    public static CodeModifierConfig? GetCodeModifierConfigFromJson(string jsonText)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
