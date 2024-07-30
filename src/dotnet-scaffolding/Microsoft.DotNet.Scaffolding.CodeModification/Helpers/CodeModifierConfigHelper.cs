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
