// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

public class EntraIdTenantIdentityPipelineTests
{
    [Fact]
    public void EntraPipeline_ValidatesIdentityBeforeMutationsAndAuthorizesBeforeSecretAndSettings()
    {
        var builder = new Mock<IScaffoldBuilder>();
        builder.SetReturnsDefault(builder.Object);
        var steps = new List<Type>();
        Capture<ValidateEntraIdStep>(builder, steps);
        Capture<ValidateAzureCliIdentityStep>(builder, steps);
        Capture<DetectBlazorWasmStep>(builder, steps);
        Capture<RegisterAppStep>(builder, steps);
        Capture<UpdateAppAuthorizationStep>(builder, steps);
        Capture<AddClientSecretStep>(builder, steps);
        Capture<UpdateAppSettingsStep>(builder, steps);
        var runner = new Mock<IScaffoldRunnerBuilder>();
        runner.Setup(r => r.AddScaffolder(It.IsAny<ScaffolderCatagory>(), It.IsAny<string>()))
            .Returns(builder.Object);
        var service = new AspNetCommandService(runner.Object);

        service.AddScaffolderCommands();

        Assert.Equal(new[]
        {
            typeof(ValidateEntraIdStep), typeof(ValidateAzureCliIdentityStep), typeof(DetectBlazorWasmStep), typeof(RegisterAppStep),
            typeof(UpdateAppAuthorizationStep), typeof(AddClientSecretStep), typeof(UpdateAppSettingsStep)
        }, steps);
        Assert.Contains(typeof(ValidateAzureCliIdentityStep), service.GetScaffoldSteps());
    }

    private static void Capture<TStep>(Mock<IScaffoldBuilder> builder, List<Type> steps) where TStep : ScaffoldStep
    {
        builder.Setup(b => b.WithStep<TStep>(It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>()))
            .Callback(() => steps.Add(typeof(TStep)))
            .Returns(builder.Object);
    }
}
