// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Contains constant values used throughout the ASP.NET scaffolding tools.
/// </summary>
internal class Constants
{
    /// <summary>
    /// File extension constants.
    /// </summary>
    public const string CSharpExtension = ".cs";
    public const string BlazorExtension = ".razor";
    public const string ViewExtension = ".cshtml";
    public const string ViewModelExtension = ".cshtml.cs";
    public const string T4TemplateExtension = ".tt";
    public const string GlobalNamespace = "<global namespace>";
    public const string NewDbContext = nameof(NewDbContext);

    /// <summary>
    /// CLI option constants for scaffolding commands.
    /// </summary>
    public class CliOptions
    {
        public const string ProjectCliOption = "--project";
        public const string PrereleaseCliOption = "--prerelease";
        public const string NameOption = "--name";
        public const string OverwriteOption = "--overwrite";
        //model with ef options
        public const string ModelCliOption = "--model";
        public const string DataContextOption = "--dataContext";
        public const string DbProviderOption = "--dbProvider";
        //crud options
        public const string PageTypeOption = "--page";
        public const string ViewsOption = "--views";
        //minimal api options
        public const string OpenApiOption = "--open";
        public const string EndpointsOption = "--endpoints";
        //controller options
        public const string ActionsOption = "--actions";
        public const string ControllerNameOption = "--controller";
        public const string UsernameOption = "--username";
        public const string TenantIdOption = "--tenantId";
        public const string ApplicationIdOption = "--applicationId";
    }

    /// <summary>
    /// Identity-related constant values.
    /// </summary>
    public class Identity
    {
        public const string UserClassName = "ApplicationUser";
        public const string DbContextName = "NewIdentityDbContext";
    }

    /// <summary>
    /// Constants for dotnet CLI commands and their default outputs.
    /// </summary>
    public class DotnetCommands
    {
        public const string RazorPageCommandName = "page";
        public const string RazorPageCommandOutput = "Pages";
        public const string RazorComponentCommandName = "razorcomponent";
        public const string RazorComponentCommandOutput = "Components";
        public const string ViewCommandName = "view";
        public const string ViewCommandOutput = "Views";
        public const string ControllerCommandOutput = "Controllers";
    }
}
