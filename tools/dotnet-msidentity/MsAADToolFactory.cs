using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.MsIdentity;

namespace Microsoft.DotNet.MsIdentity.Tool
{
    internal static class MsAADToolFactory
    {
        internal static IMsAADTool CreateTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            switch(commandName)
            {
                case Commands.LIST_AAD_APPS_COMMAND:
                case Commands.LIST_SERVICE_PRINCIPALS_COMMAND:
                    return new MsAADTool(commandName, provisioningToolOptions);
                default:
                    return new AppProvisioningTool(provisioningToolOptions);
            }
        }
    }
}