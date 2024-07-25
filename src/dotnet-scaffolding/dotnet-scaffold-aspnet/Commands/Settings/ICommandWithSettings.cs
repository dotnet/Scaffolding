// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;

internal interface ICommandWithSettings<TSettings> where TSettings : ICommandSettings
{
    Task<int> ExecuteAsync(TSettings settings, ScaffolderContext context);
}
