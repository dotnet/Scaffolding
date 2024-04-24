// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.ComponentModel
{
    public class CommandInfo
    {
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? Description { get; set; }
        public Parameter[] Parameters { get; set; } = default!;
        //add a --project option
        public bool AddProjectOption { get; set; }
    }
}
