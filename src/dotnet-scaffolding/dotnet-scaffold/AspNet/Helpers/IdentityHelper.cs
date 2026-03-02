// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Identity scaffolding, including template and output path utilities.
/// </summary>
internal static class IdentityHelper
{
    /// <summary>
    /// Use the template paths and IdentityModel to create valid 'TextTemplateProperty' objects.
    /// </summary>
    /// <param name="allFilePaths">All file paths.</param>
    /// <param name="identityModel">The identity model.</param>
    /// <returns>An enumerable of TextTemplatingProperty.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allFilePaths, IdentityModel identityModel)
    {
        if (identityModel.ProjectInfo is null || string.IsNullOrEmpty(identityModel.ProjectInfo.ProjectPath))
        {
            return [];
        }

        var textTemplatingProperties = new List<TextTemplatingProperty>();
        var templateTypes = GetIdentityTemplateTypes(identityModel.ProjectInfo.LowestSupportedTargetFramework);
        foreach (var templatePath in allFilePaths)
        {
            var templateFullName = GetFormattedRelativeIdentityFile(templatePath);
            var typeName = StringUtil.GetTypeNameFromNamespace(templateFullName);
            var templateType = templateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(templateFullName) &&
                x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            var projectName = Path.GetFileNameWithoutExtension(identityModel.ProjectInfo.ProjectPath);
            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(projectName))
            {
                string extension = string.Empty;
                //the 'ManageNavPagesModel.tt' only should have .cs extension (only exception)
                if (templateFullName.Contains("ManageNavPagesModel", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".cs";
                }
                else
                {
                    extension = templateFullName.EndsWith("Model", StringComparison.OrdinalIgnoreCase) ? ".cshtml.cs" : ".cshtml";
                }
                
                string formattedTemplateName = templateFullName.Replace("Model", string.Empty, StringComparison.OrdinalIgnoreCase);
                string templateNameWithNamespace = $"{identityModel.IdentityNamespace}.{formattedTemplateName}";
                string outputFileName = $"{StringUtil.ToPath(templateNameWithNamespace, identityModel.BaseOutputPath, projectName)}{extension}";
                textTemplatingProperties.Add(new()
                {
                    TemplateModel = identityModel,
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
    /// Gets the formatted relative identity file path from the full file name.
    /// </summary>
    /// <param name="fullFileName">The full file name.</param>
    /// <returns>The formatted relative identity file path.</returns>
    private static string GetFormattedRelativeIdentityFile(string fullFileName)
    {
        string identifier = $"Identity{Path.DirectorySeparatorChar}";
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
    /// Gets the application user text templating property.
    /// </summary>
    /// <param name="applicationUserTemplate">The application user template.</param>
    /// <param name="identityModel">The identity model.</param>
    /// <returns>A TextTemplatingProperty for the application user.</returns>
    internal static TextTemplatingProperty? GetApplicationUserTextTemplatingProperty(string? applicationUserTemplate, IdentityModel identityModel)
    {
        var projectDirectory = Path.GetDirectoryName(identityModel.ProjectInfo.ProjectPath);
        if (string.IsNullOrEmpty(applicationUserTemplate) || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        var appUserType = identityModel.ProjectInfo.LowestSupportedTargetFramework switch
        {
            TargetFramework.Net10 => typeof(Templates.net10.Files.ApplicationUser),
            TargetFramework.Net11 or _ => typeof(Templates.net11.Files.ApplicationUser),
        };

        string userClassOutputPath = $"{Path.Combine(projectDirectory, "Data", identityModel.UserClassName)}.cs";
        return new TextTemplatingProperty()
        {
            TemplateModel = identityModel,
            TemplateModelName = "Model",
            TemplatePath = applicationUserTemplate,
            TemplateType = appUserType,
            OutputPath = userClassOutputPath
        };
    }

    private static IList<Type> GetIdentityTemplateTypes(TargetFramework? targetFramework)
    {
        return targetFramework switch
        {
            TargetFramework.Net10 => _identityTemplateTypesNet10,
            TargetFramework.Net11 or _ => _identityTemplateTypesNet11,
        };
    }

    private static readonly IList<Type> _identityTemplateTypesNet10 =
    [
        typeof(Templates.net10.Identity.Pages._ViewImports),
        typeof(Templates.net10.Identity.Pages._ViewStart),
        typeof(Templates.net10.Identity.Pages.Account._StatusMessage),
        typeof(Templates.net10.Identity.Pages.Account._ViewImports),
        typeof(Templates.net10.Identity.Pages.Account.AccessDenied),
        typeof(Templates.net10.Identity.Pages.Account.AccessDeniedModel),
        typeof(Templates.net10.Identity.Pages.Account.ConfirmEmail),
        typeof(Templates.net10.Identity.Pages.Account.ConfirmEmailChange),
        typeof(Templates.net10.Identity.Pages.Account.ConfirmEmailChangeModel),
        typeof(Templates.net10.Identity.Pages.Account.ConfirmEmailModel),
        typeof(Templates.net10.Identity.Pages.Account.ExternalLogin),
        typeof(Templates.net10.Identity.Pages.Account.ExternalLoginModel),
        typeof(Templates.net10.Identity.Pages.Account.ForgotPassword),
        typeof(Templates.net10.Identity.Pages.Account.ForgotPasswordConfirmation),
        typeof(Templates.net10.Identity.Pages.Account.ForgotPasswordConfirmationModel),
        typeof(Templates.net10.Identity.Pages.Account.ForgotPasswordModel),
        typeof(Templates.net10.Identity.Pages.Account.Lockout),
        typeof(Templates.net10.Identity.Pages.Account.LockoutModel),
        typeof(Templates.net10.Identity.Pages.Account.Login),
        typeof(Templates.net10.Identity.Pages.Account.LoginModel),
        typeof(Templates.net10.Identity.Pages.Account.LoginWith2fa),
        typeof(Templates.net10.Identity.Pages.Account.LoginWith2faModel),
        typeof(Templates.net10.Identity.Pages.Account.LoginWithRecoveryCode),
        typeof(Templates.net10.Identity.Pages.Account.LoginWithRecoveryCodeModel),
        typeof(Templates.net10.Identity.Pages.Account.Logout),
        typeof(Templates.net10.Identity.Pages.Account.LogoutModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage._Layout),
        typeof(Templates.net10.Identity.Pages.Account.Manage._ManageNav),
        typeof(Templates.net10.Identity.Pages.Account.Manage._StatusMessage),
        typeof(Templates.net10.Identity.Pages.Account.Manage._ViewImports),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ChangePassword),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ChangePasswordModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.DeletePersonalData),
        typeof(Templates.net10.Identity.Pages.Account.Manage.DeletePersonalDataModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.Disable2fa),
        typeof(Templates.net10.Identity.Pages.Account.Manage.Disable2faModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.DownloadPersonalData),
        typeof(Templates.net10.Identity.Pages.Account.Manage.DownloadPersonalDataModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.Email),
        typeof(Templates.net10.Identity.Pages.Account.Manage.EmailModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.EnableAuthenticator),
        typeof(Templates.net10.Identity.Pages.Account.Manage.EnableAuthenticatorModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ExternalLogins),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ExternalLoginsModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.GenerateRecoveryCodes),
        typeof(Templates.net10.Identity.Pages.Account.Manage.GenerateRecoveryCodesModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.Index),
        typeof(Templates.net10.Identity.Pages.Account.Manage.IndexModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ManageNavPagesModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.PersonalData),
        typeof(Templates.net10.Identity.Pages.Account.Manage.PersonalDataModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ResetAuthenticator),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ResetAuthenticatorModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.SetPassword),
        typeof(Templates.net10.Identity.Pages.Account.Manage.SetPasswordModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ShowRecoveryCodes),
        typeof(Templates.net10.Identity.Pages.Account.Manage.ShowRecoveryCodesModel),
        typeof(Templates.net10.Identity.Pages.Account.Manage.TwoFactorAuthentication),
        typeof(Templates.net10.Identity.Pages.Account.Manage.TwoFactorAuthenticationModel),
        typeof(Templates.net10.Identity.Pages.Account.Register),
        typeof(Templates.net10.Identity.Pages.Account.RegisterConfirmation),
        typeof(Templates.net10.Identity.Pages.Account.RegisterConfirmationModel),
        typeof(Templates.net10.Identity.Pages.Account.RegisterModel),
        typeof(Templates.net10.Identity.Pages.Account.ResendEmailConfirmation),
        typeof(Templates.net10.Identity.Pages.Account.ResendEmailConfirmationModel),
        typeof(Templates.net10.Identity.Pages.Account.ResetPassword),
        typeof(Templates.net10.Identity.Pages.Account.ResetPasswordConfirmation),
        typeof(Templates.net10.Identity.Pages.Account.ResetPasswordConfirmationModel),
        typeof(Templates.net10.Identity.Pages.Account.ResetPasswordModel),
        typeof(Templates.net10.Identity.Pages.Error),
        typeof(Templates.net10.Identity.Pages.ErrorModel),
    ];

    private static readonly IList<Type> _identityTemplateTypesNet11 =
    [
        typeof(Templates.net11.Identity.Pages._ViewImports),
        typeof(Templates.net11.Identity.Pages._ViewStart),
        typeof(Templates.net11.Identity.Pages.Account._StatusMessage),
        typeof(Templates.net11.Identity.Pages.Account._ViewImports),
        typeof(Templates.net11.Identity.Pages.Account.AccessDenied),
        typeof(Templates.net11.Identity.Pages.Account.AccessDeniedModel),
        typeof(Templates.net11.Identity.Pages.Account.ConfirmEmail),
        typeof(Templates.net11.Identity.Pages.Account.ConfirmEmailChange),
        typeof(Templates.net11.Identity.Pages.Account.ConfirmEmailChangeModel),
        typeof(Templates.net11.Identity.Pages.Account.ConfirmEmailModel),
        typeof(Templates.net11.Identity.Pages.Account.ExternalLogin),
        typeof(Templates.net11.Identity.Pages.Account.ExternalLoginModel),
        typeof(Templates.net11.Identity.Pages.Account.ForgotPassword),
        typeof(Templates.net11.Identity.Pages.Account.ForgotPasswordConfirmation),
        typeof(Templates.net11.Identity.Pages.Account.ForgotPasswordConfirmationModel),
        typeof(Templates.net11.Identity.Pages.Account.ForgotPasswordModel),
        typeof(Templates.net11.Identity.Pages.Account.Lockout),
        typeof(Templates.net11.Identity.Pages.Account.LockoutModel),
        typeof(Templates.net11.Identity.Pages.Account.Login),
        typeof(Templates.net11.Identity.Pages.Account.LoginModel),
        typeof(Templates.net11.Identity.Pages.Account.LoginWith2fa),
        typeof(Templates.net11.Identity.Pages.Account.LoginWith2faModel),
        typeof(Templates.net11.Identity.Pages.Account.LoginWithRecoveryCode),
        typeof(Templates.net11.Identity.Pages.Account.LoginWithRecoveryCodeModel),
        typeof(Templates.net11.Identity.Pages.Account.Logout),
        typeof(Templates.net11.Identity.Pages.Account.LogoutModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage._Layout),
        typeof(Templates.net11.Identity.Pages.Account.Manage._ManageNav),
        typeof(Templates.net11.Identity.Pages.Account.Manage._StatusMessage),
        typeof(Templates.net11.Identity.Pages.Account.Manage._ViewImports),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ChangePassword),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ChangePasswordModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.DeletePersonalData),
        typeof(Templates.net11.Identity.Pages.Account.Manage.DeletePersonalDataModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.Disable2fa),
        typeof(Templates.net11.Identity.Pages.Account.Manage.Disable2faModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.DownloadPersonalData),
        typeof(Templates.net11.Identity.Pages.Account.Manage.DownloadPersonalDataModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.Email),
        typeof(Templates.net11.Identity.Pages.Account.Manage.EmailModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.EnableAuthenticator),
        typeof(Templates.net11.Identity.Pages.Account.Manage.EnableAuthenticatorModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ExternalLogins),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ExternalLoginsModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.GenerateRecoveryCodes),
        typeof(Templates.net11.Identity.Pages.Account.Manage.GenerateRecoveryCodesModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.Index),
        typeof(Templates.net11.Identity.Pages.Account.Manage.IndexModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ManageNavPagesModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.PersonalData),
        typeof(Templates.net11.Identity.Pages.Account.Manage.PersonalDataModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ResetAuthenticator),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ResetAuthenticatorModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.SetPassword),
        typeof(Templates.net11.Identity.Pages.Account.Manage.SetPasswordModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ShowRecoveryCodes),
        typeof(Templates.net11.Identity.Pages.Account.Manage.ShowRecoveryCodesModel),
        typeof(Templates.net11.Identity.Pages.Account.Manage.TwoFactorAuthentication),
        typeof(Templates.net11.Identity.Pages.Account.Manage.TwoFactorAuthenticationModel),
        typeof(Templates.net11.Identity.Pages.Account.Register),
        typeof(Templates.net11.Identity.Pages.Account.RegisterConfirmation),
        typeof(Templates.net11.Identity.Pages.Account.RegisterConfirmationModel),
        typeof(Templates.net11.Identity.Pages.Account.RegisterModel),
        typeof(Templates.net11.Identity.Pages.Account.ResendEmailConfirmation),
        typeof(Templates.net11.Identity.Pages.Account.ResendEmailConfirmationModel),
        typeof(Templates.net11.Identity.Pages.Account.ResetPassword),
        typeof(Templates.net11.Identity.Pages.Account.ResetPasswordConfirmation),
        typeof(Templates.net11.Identity.Pages.Account.ResetPasswordConfirmationModel),
        typeof(Templates.net11.Identity.Pages.Account.ResetPasswordModel),
        typeof(Templates.net11.Identity.Pages.Error),
        typeof(Templates.net11.Identity.Pages.ErrorModel),
    ];
}
