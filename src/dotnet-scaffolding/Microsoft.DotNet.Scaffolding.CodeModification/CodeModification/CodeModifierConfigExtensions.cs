// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

internal static class CodeModifierConfigExtensions
{
    internal static void EditCodeModifierConfig(this CodeModifierConfig codeModifierConfig, IDictionary<string, string> codeModifierProperties)
    {
        if (codeModifierConfig.Files is null)
        {
            return;
        }

        //modify CodeSnippets 'CheckBlock', 'Parent', and 'Block'
        var methods = codeModifierConfig.Files.SelectMany(x => x.Methods?.Values ?? Enumerable.Empty<Method>());
        var replacementCodeSnippets = codeModifierConfig.Files.SelectMany(x => x.Replacements ?? Enumerable.Empty<CodeSnippet>());
        var codeSnippets = methods.SelectMany(x => x.CodeChanges ?? Enumerable.Empty<CodeSnippet>()).ToList();
        codeSnippets.AddRange(replacementCodeSnippets);
        foreach (var codeSnippet in codeSnippets)
        {
            codeSnippet.CheckBlock = ReplaceString(codeSnippet.CheckBlock, codeModifierProperties);
            codeSnippet.Parent = ReplaceString(codeSnippet.Parent, codeModifierProperties);
            codeSnippet.Block = ReplaceString(codeSnippet.Block, codeModifierProperties) ?? codeSnippet.Block;
            if (codeSnippet.ReplaceSnippet is not null)
            {
                for (int i = 0; i < codeSnippet.ReplaceSnippet.Length; i++)
                {
                    codeSnippet.ReplaceSnippet[i] = ReplaceString(codeSnippet.ReplaceSnippet[i], codeModifierProperties) ?? codeSnippet.ReplaceSnippet[i];
                }
            }
        }

        foreach (var file in codeModifierConfig.Files)
        {
            file.Usings = ReplaceStrings(file.Usings, codeModifierProperties) ?? file.Usings;
        }
    }

    internal static string? ReplaceString(string? input, IDictionary<string, string> codeModifierProperties)
    {
        if (!string.IsNullOrEmpty(input))
        {
            foreach (var kvp in codeModifierProperties)
            {
                input = input.Replace(kvp.Key, kvp.Value);
            }
        }

        return input;
    }

        //similar to ReplaceString but for a string[]
    internal static string[]? ReplaceStrings(string[]? input, IDictionary<string, string> codeModifierProperties)
    {
        if (input is not null && input.Length > 0)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = ReplaceString(input[i], codeModifierProperties) ?? input[i];
            }
        }

        return input;
    }
}
