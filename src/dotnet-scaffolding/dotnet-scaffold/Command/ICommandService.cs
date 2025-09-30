// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Tools.Scaffold.Command
{
    internal interface ICommandService
    {
        void AddScaffolderCommands();

        List<CommandInfo> CommandInfos { get; }

        //to create parity because right now the commands are keyed by "dotnet-scaffold-aspnet" etc
        string CommandId { get; }
    }
}
