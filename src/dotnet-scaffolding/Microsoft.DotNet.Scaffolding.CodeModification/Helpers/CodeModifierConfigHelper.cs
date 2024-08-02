// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.CodeModification.Helpers;

internal static class CodeModifierConfigHelper
{
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
