// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class IdentityCodeModificationStepTests
{
    [Theory]
    [InlineData("builder.Services\n.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false);")]
    [InlineData("var services = builder.Services; services.AddIdentity<ApplicationUser, IdentityRole>();")]
    [InlineData("builder.Services.AddIdentityCore<ApplicationUser>();")]
    public void FindIdentityRegistration_RecognizesCallsRegardlessOfFormatting(string source)
        => Assert.NotNull(IdentityHelper.FindIdentityRegistration(CSharpSyntaxTree.ParseText(source).GetRoot()));

    [Theory]
    [InlineData("// builder.Services.AddIdentity<ApplicationUser, IdentityRole>();")]
    [InlineData("var example = \"builder.Services.AddIdentityCore<ApplicationUser>()\";")]
    [InlineData("builder.Services.AddAuthentication().AddIdentityCookies();")]
    public void FindIdentityRegistration_IgnoresNonRegistrations(string source)
        => Assert.Null(IdentityHelper.FindIdentityRegistration(CSharpSyntaxTree.ParseText(source).GetRoot()));

    [Theory]
    [InlineData("AddIdentity<ApplicationUser, IdentityRole>", 3)]
    [InlineData("AddIdentityCore<ApplicationUser>", 8)]
    [InlineData("AddDefaultIdentity<ApplicationUser>", 1)]
    public void GetMissingIdentityChanges_CompletesPartialRegistrations(string method, int expectedChanges)
    {
        var root = CSharpSyntaxTree.ParseText($"builder.Services.{method}();").GetRoot();
        var registration = IdentityHelper.FindIdentityRegistration(root)!;
        var changes = IdentityCodeModificationStep.GetMissingIdentityChanges(root, registration).ToList();

        Assert.Equal(expectedChanges, changes.Count);
        Assert.DoesNotContain(changes, change => JsonSerializer.Serialize(change).Contains("\"Block\":\"builder.Services.AddDefaultIdentity"));
    }

    [Fact]
    public void GetMissingIdentityChanges_PreservesExistingStore()
    {
        var root = CSharpSyntaxTree.ParseText("""
builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddEntityFrameworkStores<CustomDbContext>();
""").GetRoot();
        Assert.Empty(IdentityCodeModificationStep.GetMissingIdentityChanges(root, IdentityHelper.FindIdentityRegistration(root)!));
    }

    [Fact]
    public void GetMissingIdentityChanges_PreservesCustomCookiesAndTokenProviders()
    {
        var root = CSharpSyntaxTree.ParseText("""
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddDefaultUI()
    .AddTokenProvider<CustomProvider>("Default");
builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme);
""").GetRoot();
        var changes = IdentityCodeModificationStep.GetMissingIdentityChanges(root, IdentityHelper.FindIdentityRegistration(root)!);
        var json = JsonSerializer.Serialize(changes);

        Assert.DoesNotContain("AddApplicationCookie", json);
        Assert.DoesNotContain("AddDefaultTokenProviders", json);
        Assert.DoesNotContain("AddDefaultUI()", json);
        Assert.Contains("AddExternalCookie", json);
        Assert.Contains("AddTwoFactorRememberMeCookie", json);
        Assert.Contains("AddTwoFactorUserIdCookie", json);
    }
}
