// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;

internal static class MinimalApiHelper
{
    internal static Type? GetMinimalApiTemplateType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        switch (Path.GetFileName(templatePath))
        {
            case "MinimalApi.tt":
                return typeof(Templates.MinimalApi.MinimalApi);
            case "MinimalApiEf.tt":
                return typeof(Templates.MinimalApi.MinimalApiEf);
            default:
                break;
        }

        return null;
    }
}
