// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.CodeModification.Helpers;

public static class CodeModifierConfigHelper
{
    public static CodeModifierConfig? GetCodeModifierConfig(string configName, Assembly? assembly = null)
    {
        assembly = assembly ?? Assembly.GetExecutingAssembly();
        string jsonText = ProjectModelHelper.GetManifestResource(assembly, shortResourceName: configName);
        return System.Text.Json.JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
    }
}
