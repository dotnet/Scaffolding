// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

internal static class CommandInfoExtensions
{
    public static bool IsCommandAnAspireCommand(this CommandInfo commandInfo)
    {
        return commandInfo.DisplayCategories.Any(category => string.Equals(category, "Aspire", StringComparison.Ordinal));
    }

    //TODO improve this logic, maybe put the scaffolder catagory in the commandInfo?
    public static bool IsCommandAnAspNetCommand(this CommandInfo commandInfo)
    {
        return commandInfo.DisplayCategories.Any(category => string.Equals(category, "Blazor", StringComparison.Ordinal) ||
            string.Equals(category, "MVC", StringComparison.Ordinal) ||
            string.Equals(category, "Razor Pages", StringComparison.Ordinal) ||
            string.Equals(category, "API", StringComparison.Ordinal) ||
            string.Equals(category, "Identity", StringComparison.Ordinal) ||
            string.Equals(category, "Entra ID", StringComparison.Ordinal));
    }
}
