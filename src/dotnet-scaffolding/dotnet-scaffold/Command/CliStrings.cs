// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Command
{
    internal static class CliStrings
    {
        internal const string NonInteractiveCliOption = "--non-interactive";

        //tool commands
        internal const string ToolCommand = "tool";
        internal const string ToolCommandDescription = "Manage installed scaffold tools";

        internal const string ToolInstallSubcommand = "install";
        internal const string ToolInstallSubcommandDescription = "Install a scaffold tool";

        internal const string ToolUninstallSubcommand = "uninstall";
        internal const string ToolUninstallSubcommandDescription = "Uninstall a scaffold tool";

        internal const string ToolListSubcommand = "list";
        internal const string ToolListSubcommandDescription = "List installed scaffold tools";
    }
}
