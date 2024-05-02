// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.ComponentModel
{
    /// <summary>
    /// What we want components to initialize and return (to stdout serialized as json) when 'get-commands' is invoked.
    /// Should be serialized as 'List<CommandInfo>'
    /// </summary>
    public class CommandInfo
    {
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? Description { get; set; }
        public Parameter[] Parameters { get; set; } = default!;
    }
}
