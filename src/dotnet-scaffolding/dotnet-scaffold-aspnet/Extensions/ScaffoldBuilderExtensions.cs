// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;

internal static class ScaffoldBuilderExtensions
{
    public static IScaffoldBuilder WithDotnetNewScaffolderStep(this IScaffoldBuilder builder, ScaffolderOption<string> projectOption, ScaffolderOption<string> fileNameOption, string commandName)
    {
        return builder.WithStep<DotnetNewScaffolderStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            step.Project = context.GetOptionResult(projectOption);
            step.Name = context.GetOptionResult(fileNameOption);
            step.CommandName = commandName;
        });
    }
}
