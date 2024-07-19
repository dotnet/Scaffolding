// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;

internal class EmptyControllerCommandSettings : DotnetNewCommandSettings
{
    public required bool Actions { get; init; }
}
