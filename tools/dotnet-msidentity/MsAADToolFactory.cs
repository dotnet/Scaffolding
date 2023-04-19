// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.MSIdentity.Tool
{
    internal static class MsAADToolFactory
    {
        internal static IMsAADTool CreateTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            switch (commandName)
            {
                case Commands.LIST_AAD_APPS_COMMAND:
                case Commands.LIST_SERVICE_PRINCIPALS_COMMAND:
                case Commands.LIST_TENANTS_COMMAND:
                    return new MsAADTool(commandName, provisioningToolOptions);
                default:
                    return new AppProvisioningTool(commandName, provisioningToolOptions);
            }
        }
    }
}
