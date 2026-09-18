// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Adds the Identity login partial and references it from the host application's layout.
/// </summary>
internal class ConfigureIdentityNavigationStep(
    ILogger<ConfigureIdentityNavigationStep> logger,
    IFileSystem fileSystem) : ScaffoldStep
{
    public required string ProjectPath { get; set; }
    public required string UserClassName { get; set; }
    public required string UserClassNamespace { get; set; }
    public bool IsRazorPages { get; set; }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var projectDirectory = Path.GetDirectoryName(ProjectPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            logger.LogError("Unable to determine the project directory while configuring Identity navigation.");
            return Task.FromResult(false);
        }

        var sharedDirectory = Path.Combine(projectDirectory, IsRazorPages ? "Pages" : "Views", "Shared");
        var layoutPath = Path.Combine(sharedDirectory, "_Layout.cshtml");
        if (!fileSystem.FileExists(layoutPath))
        {
            logger.LogWarning($"Identity navigation was not added because '{layoutPath}' does not exist.");
            return Task.FromResult(true);
        }

        var layoutContent = fileSystem.ReadAllText(layoutPath);
        if (layoutContent.Contains("_LoginPartial", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true);
        }

        var navbarClassIndex = layoutContent.IndexOf("navbar-nav", StringComparison.OrdinalIgnoreCase);
        var openingListIndex = navbarClassIndex < 0
            ? -1
            : layoutContent.LastIndexOf("<ul", navbarClassIndex, StringComparison.OrdinalIgnoreCase);
        var closingListIndex = openingListIndex < 0
            ? -1
            : layoutContent.IndexOf("</ul>", navbarClassIndex, StringComparison.OrdinalIgnoreCase);
        if (closingListIndex < 0)
        {
            logger.LogWarning($"Identity navigation was not added to '{layoutPath}' because no navbar navigation list was found.");
            return Task.FromResult(true);
        }

        fileSystem.CreateDirectoryIfNotExists(sharedDirectory);
        var loginPartialPath = Path.Combine(sharedDirectory, "_LoginPartial.cshtml");
        if (!fileSystem.FileExists(loginPartialPath))
        {
            fileSystem.WriteAllText(loginPartialPath, GetLoginPartialContent());
        }

        var lineStartIndex = layoutContent.LastIndexOf('\n', closingListIndex);
        lineStartIndex = lineStartIndex < 0 ? 0 : lineStartIndex + 1;
        var indentation = layoutContent[lineStartIndex..closingListIndex];
        if (indentation.Any(character => !char.IsWhiteSpace(character)))
        {
            indentation = string.Empty;
        }

        var newline = layoutContent.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var insertionIndex = closingListIndex + "</ul>".Length;
        layoutContent = layoutContent.Insert(insertionIndex, $"{newline}{indentation}<partial name=\"_LoginPartial\" />");
        fileSystem.WriteAllText(layoutPath, layoutContent);

        return Task.FromResult(true);
    }

    private string GetLoginPartialContent()
    {
        var returnUrl = IsRazorPages
            ? "@Url.Page(\"/Index\", new { area = \"\" })"
            : "@Url.Action(\"Index\", \"Home\", new { area = \"\" })";

        return $$"""
@using Microsoft.AspNetCore.Identity
@using {{UserClassNamespace}}
@inject SignInManager<{{UserClassName}}> SignInManager
@inject UserManager<{{UserClassName}}> UserManager

<ul class="navbar-nav">
@if (SignInManager.IsSignedIn(User))
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="Identity" asp-page="/Account/Manage/Index" title="Manage">Hello @User.Identity?.Name!</a>
    </li>
    <li class="nav-item">
        <form class="form-inline" asp-area="Identity" asp-page="/Account/Logout" asp-route-returnUrl="{{returnUrl}}">
            <button type="submit" class="nav-link btn btn-link text-dark">Logout</button>
        </form>
    </li>
}
else
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="Identity" asp-page="/Account/Register">Register</a>
    </li>
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="Identity" asp-page="/Account/Login">Login</a>
    </li>
}
</ul>
""";
    }
}
