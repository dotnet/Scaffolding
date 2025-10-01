// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

internal static class CommandInfoExtensions
{
    public static bool IsCommandAnAspireCommand(this CommandInfo commandInfo)
    {
        return commandInfo.DisplayCategories.Any(category => string.Equals(category, "Aspire", StringComparison.Ordinal));
    }
}
