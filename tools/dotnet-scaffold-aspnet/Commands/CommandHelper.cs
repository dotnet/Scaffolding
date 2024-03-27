// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands
{
    internal static class CommandHelper
    {
    }

    public class ProjectScaffolderSettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string Project { get; set; } = default!;
    }
}
