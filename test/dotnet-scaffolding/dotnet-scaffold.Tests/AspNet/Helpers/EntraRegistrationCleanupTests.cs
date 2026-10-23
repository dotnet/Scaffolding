// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class EntraRegistrationCleanupTests
{
    [Fact]
    public void TryCleanup_NeverDeletesExistingApplication()
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties["ClientId"] = "existing-app";
        var executed = false;
        EntraRegistrationCleanup.TryCleanup(context, "tenant-a", "user@example.com", NullLogger.Instance,
            _ => { executed = true; return (0, null); });
        Assert.False(executed);
        Assert.Equal("existing-app", context.Properties["ClientId"]);
    }

    [Fact]
    public void TryCleanup_RequiresExplicitIdentity()
    {
        var context = CreateContext();
        var executed = false;
        EntraRegistrationCleanup.TryCleanup(context, null, null, NullLogger.Instance,
            _ => { executed = true; return (0, null); });

        Assert.False(executed);
        Assert.Equal("created-app", context.Properties[EntraRegistrationCleanup.CreatedClientIdKey]);
    }

    [Fact]
    public void TryCleanup_DeletesOnlyCreatedApplicationUsingSelectedIdentity()
    {
        var context = CreateContext();
        string[]? arguments = null;
        EntraRegistrationCleanup.TryCleanup(context, "tenant-a", "user@example.com", NullLogger.Instance, args =>
        {
            arguments = args;
            return (0, null);
        });

        Assert.Equal(new[] { "--unregister-app", "--client-id", "created-app", "--tenant-id", "tenant-a",
            "--username", "user@example.com" }, arguments);
        Assert.Empty(context.Properties);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryCleanup_ReportsFailureAndRetainsResourceIdentity(bool throws)
    {
        var context = CreateContext();
        var logger = new Mock<ILogger>();
        var failure = new InvalidOperationException("Failed to start tool.");

        EntraRegistrationCleanup.TryCleanup(context, "tenant-a", "user@example.com", logger.Object,
            _ => throws ? throw failure : (1, "Insufficient privileges to complete the operation."));

        var error = Assert.Single(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments[0] is LogLevel.Error);
        Assert.Contains("created-app", error.Arguments[2].ToString());
        Assert.Contains("tenant-a", error.Arguments[2].ToString());
        if (throws)
        {
            Assert.Same(failure, error.Arguments[3]);
        }
        else
        {
            Assert.Contains("Insufficient privileges", error.Arguments[2].ToString());
        }
        Assert.Equal("created-app", context.Properties[EntraRegistrationCleanup.CreatedClientIdKey]);
        Assert.Equal("created-app", context.Properties["ClientId"]);
    }

    private static ScaffolderContext CreateContext()
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties[EntraRegistrationCleanup.CreatedClientIdKey] = "created-app";
        context.Properties["ClientId"] = "created-app";
        context.Properties["ClientSecret"] = "development-secret";
        return context;
    }
}
