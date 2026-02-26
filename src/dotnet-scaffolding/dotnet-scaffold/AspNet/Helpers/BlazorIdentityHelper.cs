// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

//TODO : combine with 'IdentityHelper', should be quite easy.
/// <summary>
/// Helper methods for Blazor Identity scaffolding, including template and output path utilities.
/// </summary>
internal static class BlazorIdentityHelper
{
    /// <summary>
    /// Retrieves the text templating properties for the given T4 templates and Blazor identity model.
    /// </summary>
    /// <param name="allT4TemplatePaths">The paths of all T4 templates.</param>
    /// <param name="blazorIdentityModel">The Blazor identity model containing project and identity information.</param>
    /// <returns>An <see cref="IEnumerable{TextTemplatingProperty}"/> collection containing the text templating properties for the specified templates.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, IdentityModel blazorIdentityModel)
    {
        if (blazorIdentityModel.ProjectInfo is null || string.IsNullOrEmpty(blazorIdentityModel.ProjectInfo.ProjectPath))
        {
            return [];
        }

        var textTemplatingProperties = new List<TextTemplatingProperty>();
        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateFullName = GetFormattedRelativeIdentityFile(templatePath);
            var typeName = StringUtil.GetTypeNameFromNamespace(templateFullName);
            var templateTypes = GetBlazorIdentityTemplateTypes(blazorIdentityModel.ProjectInfo.LowestSupportedTargetFramework);
            var templateType = templateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(templateFullName) &&
                x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            var projectName = Path.GetFileNameWithoutExtension(blazorIdentityModel.ProjectInfo.ProjectPath);

            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(projectName))
            {
                // Files in Pages and Shared folders are Razor components, others are C# files
                string extension = templateFullName.StartsWith("Pages", StringComparison.OrdinalIgnoreCase) ||
                                   templateFullName.StartsWith("Shared", StringComparison.OrdinalIgnoreCase) ? ".razor" : ".cs";
                string templateNameWithNamespace = $"{blazorIdentityModel.IdentityNamespace}.{templateFullName}";
                string outputFileName = $"{StringUtil.ToPath(templateNameWithNamespace, blazorIdentityModel.BaseOutputPath, projectName)}{extension}";
                textTemplatingProperties.Add(new()
                {
                    TemplateModel = blazorIdentityModel,
                    TemplateModelName = "Model",
                    TemplatePath = templatePath,
                    TemplateType = templateType,
                    OutputPath = outputFileName
                });
            }
        }

        return textTemplatingProperties;
    }

    /// <summary>
    /// Retrieves the formatted relative identity file path from the full file name.
    /// </summary>
    /// <param name="fullFileName">The full file name to retrieve the relative identity file path from.</param>
    /// <returns>The formatted relative identity file path.</returns>
    private static string GetFormattedRelativeIdentityFile(string fullFileName)
    {
        string identifier = $"BlazorIdentity{Path.DirectorySeparatorChar}";
        int index = fullFileName.IndexOf(identifier);
        if (index != -1)
        {
            string pathAfterIdentifier = fullFileName.Substring(index + identifier.Length);
            string pathAsNamespaceWithoutExtension = StringUtil.GetFilePathWithoutExtension(pathAfterIdentifier);
            return pathAsNamespaceWithoutExtension;
        }

        return string.Empty;
    }

    /// <summary>
    /// Retrieves the text templating property for the application user template.
    /// </summary>
    /// <param name="applicationUserTemplate">The path of the application user template.</param>
    /// <param name="blazorIdentityModel">The Blazor identity model containing project and identity information.</param>
    /// <returns>A <see cref="TextTemplatingProperty"/> object containing the text templating property for the application user template, or null if the template path or project directory is invalid.</returns>
    internal static TextTemplatingProperty? GetApplicationUserTextTemplatingProperty(string? applicationUserTemplate, IdentityModel blazorIdentityModel)
    {
        var projectDirectory = Path.GetDirectoryName(blazorIdentityModel.ProjectInfo.ProjectPath);
        if (string.IsNullOrEmpty(applicationUserTemplate) || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        string userClassOutputPath = $"{Path.Combine(projectDirectory, "Data", blazorIdentityModel.UserClassName)}.cs";
        var targetFramework = blazorIdentityModel.ProjectInfo.LowestSupportedTargetFramework;
        Type appUserType = targetFramework switch
        {
            TargetFramework.Net9 => typeof(Templates.Files.ApplicationUser),
            TargetFramework.Net8 or TargetFramework.Net10 or TargetFramework.Net11 or _ => typeof(Templates.net10.Files.ApplicationUser),
        };
        return new TextTemplatingProperty()
        {
            TemplateModel = blazorIdentityModel,
            TemplateModelName = "Model",
            TemplatePath = applicationUserTemplate,
            TemplateType = appUserType,
            OutputPath = userClassOutputPath
        };
    }

    /// <summary>
    /// Gets the Blazor Identity template types for the specified framework version.
    /// </summary>
    private static IList<Type> GetBlazorIdentityTemplateTypes(TargetFramework? targetFramework)
    {
        switch (targetFramework)
        {
            case TargetFramework.Net9:
                return _blazorIdentityTemplateTypesNet9;
            case TargetFramework.Net8:
            case TargetFramework.Net10:
            case TargetFramework.Net11:
            default:
                return _blazorIdentityTemplateTypesNet10;
        }
    }

    private static readonly IList<Type> _blazorIdentityTemplateTypesNet9 =
    [
        typeof(Templates.BlazorIdentity.IdentityComponentsEndpointRouteBuilderExtensions),
        typeof(Templates.BlazorIdentity.IdentityNoOpEmailSender),
        typeof(Templates.BlazorIdentity.IdentityRedirectManager),
        typeof(Templates.BlazorIdentity.IdentityRevalidatingAuthenticationStateProvider),
        typeof(Templates.BlazorIdentity.IdentityUserAccessor),
        typeof(Templates.BlazorIdentity.Pages._Imports),
        typeof(Templates.BlazorIdentity.Pages.AccessDenied),
        typeof(Templates.BlazorIdentity.Pages.ConfirmEmail),
        typeof(Templates.BlazorIdentity.Pages.ConfirmEmailChange),
        typeof(Templates.BlazorIdentity.Pages.ExternalLogin),
        typeof(Templates.BlazorIdentity.Pages.ForgotPassword),
        typeof(Templates.BlazorIdentity.Pages.ForgotPasswordConfirmation),
        typeof(Templates.BlazorIdentity.Pages.InvalidPasswordReset),
        typeof(Templates.BlazorIdentity.Pages.InvalidUser),
        typeof(Templates.BlazorIdentity.Pages.Lockout),
        typeof(Templates.BlazorIdentity.Pages.Login),
        typeof(Templates.BlazorIdentity.Pages.LoginWith2fa),
        typeof(Templates.BlazorIdentity.Pages.LoginWithRecoveryCode),
        typeof(Templates.BlazorIdentity.Pages.Manage._Imports),
        typeof(Templates.BlazorIdentity.Pages.Manage.ChangePassword),
        typeof(Templates.BlazorIdentity.Pages.Manage.DeletePersonalData),
        typeof(Templates.BlazorIdentity.Pages.Manage.Disable2fa),
        typeof(Templates.BlazorIdentity.Pages.Manage.Email),
        typeof(Templates.BlazorIdentity.Pages.Manage.EnableAuthenticator),
        typeof(Templates.BlazorIdentity.Pages.Manage.ExternalLogins),
        typeof(Templates.BlazorIdentity.Pages.Manage.GenerateRecoveryCodes),
        typeof(Templates.BlazorIdentity.Pages.Manage.Index),
        typeof(Templates.BlazorIdentity.Pages.Manage.PersonalData),
        typeof(Templates.BlazorIdentity.Pages.Manage.ResetAuthenticator),
        typeof(Templates.BlazorIdentity.Pages.Manage.SetPassword),
        typeof(Templates.BlazorIdentity.Pages.Manage.TwoFactorAuthentication),
        typeof(Templates.BlazorIdentity.Pages.Register),
        typeof(Templates.BlazorIdentity.Pages.RegisterConfirmation),
        typeof(Templates.BlazorIdentity.Pages.ResendEmailConfirmation),
        typeof(Templates.BlazorIdentity.Pages.ResetPassword),
        typeof(Templates.BlazorIdentity.Pages.ResetPasswordConfirmation),
        typeof(Templates.BlazorIdentity.Shared.AccountLayout),
        typeof(Templates.BlazorIdentity.Shared.ExternalLoginPicker),
        typeof(Templates.BlazorIdentity.Shared.ManageLayout),
        typeof(Templates.BlazorIdentity.Shared.ManageNavMenu),
        typeof(Templates.BlazorIdentity.Shared.RedirectToLogin),
        typeof(Templates.BlazorIdentity.Shared.ShowRecoveryCodes),
        typeof(Templates.BlazorIdentity.Shared.StatusMessage),
    ];

    private static readonly IList<Type> _blazorIdentityTemplateTypesNet10 =
    [
        typeof(Templates.net10.BlazorIdentity.IdentityComponentsEndpointRouteBuilderExtensions),
        typeof(Templates.net10.BlazorIdentity.IdentityNoOpEmailSender),
        typeof(Templates.net10.BlazorIdentity.IdentityRedirectManager),
        typeof(Templates.net10.BlazorIdentity.IdentityRevalidatingAuthenticationStateProvider),
        typeof(Templates.net10.BlazorIdentity.PasskeyInputModel),
        typeof(Templates.net10.BlazorIdentity.PasskeyOperation),
        typeof(Templates.net10.BlazorIdentity.Pages._Imports),
        typeof(Templates.net10.BlazorIdentity.Pages.AccessDenied),
        typeof(Templates.net10.BlazorIdentity.Pages.ConfirmEmail),
        typeof(Templates.net10.BlazorIdentity.Pages.ConfirmEmailChange),
        typeof(Templates.net10.BlazorIdentity.Pages.ExternalLogin),
        typeof(Templates.net10.BlazorIdentity.Pages.ForgotPassword),
        typeof(Templates.net10.BlazorIdentity.Pages.ForgotPasswordConfirmation),
        typeof(Templates.net10.BlazorIdentity.Pages.InvalidPasswordReset),
        typeof(Templates.net10.BlazorIdentity.Pages.InvalidUser),
        typeof(Templates.net10.BlazorIdentity.Pages.Lockout),
        typeof(Templates.net10.BlazorIdentity.Pages.Login),
        typeof(Templates.net10.BlazorIdentity.Pages.LoginWith2fa),
        typeof(Templates.net10.BlazorIdentity.Pages.LoginWithRecoveryCode),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage._Imports),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.ChangePassword),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.DeletePersonalData),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.Disable2fa),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.Email),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.EnableAuthenticator),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.ExternalLogins),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.GenerateRecoveryCodes),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.Index),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.Passkeys),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.PersonalData),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.RenamePasskey),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.ResetAuthenticator),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.SetPassword),
        typeof(Templates.net10.BlazorIdentity.Pages.Manage.TwoFactorAuthentication),
        typeof(Templates.net10.BlazorIdentity.Pages.Register),
        typeof(Templates.net10.BlazorIdentity.Pages.RegisterConfirmation),
        typeof(Templates.net10.BlazorIdentity.Pages.ResendEmailConfirmation),
        typeof(Templates.net10.BlazorIdentity.Pages.ResetPassword),
        typeof(Templates.net10.BlazorIdentity.Pages.ResetPasswordConfirmation),
        typeof(Templates.net10.BlazorIdentity.Shared.ExternalLoginPicker),
        typeof(Templates.net10.BlazorIdentity.Shared.ManageLayout),
        typeof(Templates.net10.BlazorIdentity.Shared.ManageNavMenu),
        typeof(Templates.net10.BlazorIdentity.Shared.PasskeySubmit),
        typeof(Templates.net10.BlazorIdentity.Shared.RedirectToLogin),
        typeof(Templates.net10.BlazorIdentity.Shared.ShowRecoveryCodes),
        typeof(Templates.net10.BlazorIdentity.Shared.StatusMessage),
    ];
}
