// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public class ScaffolderOptionConfig
{
    public required string DisplayName { get; init; }
    public string? CliOption { get; set; }
    public bool Required { get; set; }
    public string? Description { get; set; }
    public InteractivePickerType PickerType { get; set; } = InteractivePickerType.None;
    public IEnumerable<string>? CustomPickerValues { get; init; }
}
