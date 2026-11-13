// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using IdentityTemplates = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

public class IdentityEmailTemplateTests
{
    [Theory]
    [InlineData(typeof(IdentityTemplates.net8.BlazorIdentity.Pages.ForgotPassword))]
    [InlineData(typeof(IdentityTemplates.net8.BlazorIdentity.Pages.ResendEmailConfirmation))]
    [InlineData(typeof(IdentityTemplates.net9.BlazorIdentity.Pages.ForgotPassword))]
    [InlineData(typeof(IdentityTemplates.net9.BlazorIdentity.Pages.ResendEmailConfirmation))]
    [InlineData(typeof(IdentityTemplates.net10.BlazorIdentity.Pages.ForgotPassword))]
    [InlineData(typeof(IdentityTemplates.net10.BlazorIdentity.Pages.ResendEmailConfirmation))]
    [InlineData(typeof(IdentityTemplates.net11.BlazorIdentity.Pages.ForgotPassword))]
    [InlineData(typeof(IdentityTemplates.net11.BlazorIdentity.Pages.ResendEmailConfirmation))]
    [InlineData(typeof(IdentityTemplates.net9.Identity.Pages.Account.ForgotPasswordModel))]
    [InlineData(typeof(IdentityTemplates.net9.Identity.Pages.Account.ResendEmailConfirmationModel))]
    [InlineData(typeof(IdentityTemplates.net10.Identity.Pages.Account.ForgotPasswordModel))]
    [InlineData(typeof(IdentityTemplates.net10.Identity.Pages.Account.ResendEmailConfirmationModel))]
    [InlineData(typeof(IdentityTemplates.net11.Identity.Pages.Account.ForgotPasswordModel))]
    [InlineData(typeof(IdentityTemplates.net11.Identity.Pages.Account.ResendEmailConfirmationModel))]
    public void PrecompiledTemplate_SendsToStoredEmailAndRejectsMissingEmail(Type templateType)
    {
        var model = new IdentityModel
        {
            DbContextInfo = new DbContextInfo { DbContextNamespace = "TestApp.Data" },
            ProjectInfo = new ProjectInfo(Path.Combine("TestApp", "TestApp.csproj")),
            IdentityNamespace = "TestApp.Areas.Identity",
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestApp.Data",
            DbContextNamespace = "TestApp.Data",
            BaseOutputPath = "TestApp"
        };
        var parameters = new Dictionary<string, object> { { "Model", model } };
        var template = Activator.CreateInstance(templateType);
        string result;
        if (template is ITextTransformation transformation)
        {
            transformation.Session = parameters;
            transformation.Initialize();
            result = transformation.TransformText();
            Assert.False(transformation.Errors.HasErrors);
        }
        else
        {
            var sharedTransformation = Assert.IsAssignableFrom<Microsoft.DotNet.Scaffolding.Shared.T4Templating.ITextTransformation>(template);
            sharedTransformation.Session = parameters;
            sharedTransformation.Initialize();
            result = sharedTransformation.TransformText();
            Assert.False(sharedTransformation.Errors.HasErrors);
        }

        bool isBlazor = templateType.Namespace!.Contains(".BlazorIdentity.", StringComparison.Ordinal);
        AssertUsesStoredEmail(result, isBlazor, templateType.Name.StartsWith("ForgotPassword", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(@"src\Scaffolding\VS.Web.CG.Mvc\Templates")]
    [InlineData(@"src\dotnet-scaffolding\dotnet-scaffold\AspNet\Templates\net8.0")]
    [InlineData(@"src\dotnet-scaffolding\dotnet-scaffold\AspNet\Templates\net9.0")]
    [InlineData(@"src\dotnet-scaffolding\dotnet-scaffold\AspNet\Templates\net10.0")]
    [InlineData(@"src\dotnet-scaffolding\dotnet-scaffold\AspNet\Templates\net11.0")]
    public void BlazorTemplateSource_SendsToStoredEmailAndRejectsMissingEmail(string templatesDirectory)
    {
        var directory = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), templatesDirectory.Replace('\\', Path.DirectorySeparatorChar),
            "BlazorIdentity", "Pages");
        AssertUsesStoredEmail(File.ReadAllText(Path.Combine(directory, "ForgotPassword.tt")), isBlazor: true, forgotPassword: true);
        AssertUsesStoredEmail(File.ReadAllText(Path.Combine(directory, "ResendEmailConfirmation.tt")), isBlazor: true, forgotPassword: false);
    }

    [Theory]
    [InlineData(@"src\Scaffolding\VS.Web.CG.Mvc\Templates\Identity\Bootstrap4")]
    [InlineData(@"src\Scaffolding\VS.Web.CG.Mvc\Templates\Identity\Bootstrap5")]
    [InlineData(@"src\dotnet-scaffolding\dotnet-scaffold\AspNet\Templates\net8.0\Identity\Bootstrap4")]
    [InlineData(@"src\dotnet-scaffolding\dotnet-scaffold\AspNet\Templates\net8.0\Identity\Bootstrap5")]
    public void RazorTemplateSource_SendsToStoredEmailAndRejectsMissingEmail(string templatesDirectory)
    {
        var directory = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), templatesDirectory.Replace('\\', Path.DirectorySeparatorChar),
            "Pages", "Account");
        AssertUsesStoredEmail(File.ReadAllText(Path.Combine(directory, "Account.ForgotPassword.cs.cshtml")), isBlazor: false, forgotPassword: true);
        AssertUsesStoredEmail(File.ReadAllText(Path.Combine(directory, "Account.ResendEmailConfirmation.cs.cshtml")), isBlazor: false, forgotPassword: false);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void RazorT4TemplateSource_SendsToStoredEmailAndRejectsMissingEmail(int frameworkVersion)
    {
        var directory = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", $"net{frameworkVersion}.0", "Identity", "Pages", "Account");
        AssertUsesStoredEmail(File.ReadAllText(Path.Combine(directory, "ForgotPasswordModel.tt")), isBlazor: false, forgotPassword: true);
        AssertUsesStoredEmail(File.ReadAllText(Path.Combine(directory, "ResendEmailConfirmationModel.tt")), isBlazor: false, forgotPassword: false);
    }

    private static void AssertUsesStoredEmail(string template, bool isBlazor, bool forgotPassword)
    {
        string userManager = isBlazor ? "UserManager" : "_userManager";
        string sender = isBlazor ? "EmailSender" : "_emailSender";
        string sendMethod = isBlazor
            ? (forgotPassword ? "SendPasswordResetLinkAsync" : "SendConfirmationLinkAsync")
            : "SendEmailAsync";

        Assert.Contains($"{userManager}.FindByEmailAsync(Input.Email", template);
        string lookup = $"var storedEmail = await {userManager}.GetEmailAsync(user);";
        string guard = "if (string.IsNullOrEmpty(storedEmail))";
        string failure = "throw new InvalidOperationException(\"Users must have an email.\");";
        template = template.Replace("\r\n", "\n");
        Assert.Contains(lookup, template);
        Assert.Contains(guard, template);
        Assert.Contains(failure, template);
        if (isBlazor)
        {
            Assert.Contains($"await {sender}.{sendMethod}(user, storedEmail,", template);
        }
        else
        {
            Assert.Matches(@"await _emailSender\.SendEmailAsync\(\s+storedEmail,", template);
        }

        int lookupIndex = template.IndexOf(lookup, StringComparison.Ordinal);
        int guardIndex = template.IndexOf(guard, StringComparison.Ordinal);
        int failureIndex = template.IndexOf(failure, StringComparison.Ordinal);
        int sendIndex = template.IndexOf($"await {sender}.{sendMethod}(", StringComparison.Ordinal);
        Assert.True(lookupIndex < guardIndex && guardIndex < failureIndex && failureIndex < sendIndex);
        Assert.DoesNotMatch(@"Send\w+Async\(\s*(?:user,\s*)?Input\.Email", template);
    }
}
