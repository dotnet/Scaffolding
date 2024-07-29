// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public abstract class ScaffolderOption
{
    public required string DisplayName { get; init; }
    public string? CliOption { get; init; }
    public bool Required { get; init; }
    public string? Description { get; init; }
    public InteractivePickerType PickerType { get; init; } = InteractivePickerType.None;
    public IEnumerable<string>? CustomPickerValues { get; init; } = null;

    internal abstract Option ToCliOption();
    internal abstract Parameter ToParameter();
}
