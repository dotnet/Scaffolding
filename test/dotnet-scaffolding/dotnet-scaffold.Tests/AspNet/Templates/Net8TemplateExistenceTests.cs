// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

/// <summary>
/// Tests to verify all required template files exist in the net8.0 folder structure.
/// Note: Net8.0 only has .tt files for BlazorCrud, BlazorIdentity (non-Passkey), and Files templates.
/// Other templates (EfController, MinimalApi, RazorPages, Views, Identity) use .cshtml format in net8.0.
/// </summary>
public class Net8TemplateExistenceTests
{
    private static string GetTemplateBasePath()
    {
        // Get the assembly location and navigate to the template directory
        var assemblyLocation = typeof(Net8TemplateExistenceTests).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        
        // Navigate up from test directory to find src directory
        var testDir = assemblyDir;
        while (testDir != null && !Directory.Exists(Path.Combine(testDir, "src")))
        {
            testDir = Directory.GetParent(testDir)?.FullName;
        }
        
        Assert.NotNull(testDir);
        return Path.Combine(testDir!, "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates", "net8.0");
    }

    #region BlazorCrud Templates

    [Fact]
    public void BlazorCrud_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorCrud", "Create.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorCrud_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorCrud", "Delete.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorCrud_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorCrud", "Details.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorCrud_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorCrud", "Edit.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorCrud_IndexTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorCrud", "Index.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region BlazorIdentity Core Templates

    [Fact]
    public void BlazorIdentity_IdentityComponentsEndpointRouteBuilderExtensionsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "IdentityComponentsEndpointRouteBuilderExtensions.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_IdentityNoOpEmailSenderTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "IdentityNoOpEmailSender.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_IdentityRedirectManagerTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "IdentityRedirectManager.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_IdentityRevalidatingAuthenticationStateProviderTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "IdentityRevalidatingAuthenticationStateProvider.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_IdentityUserAccessorTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "IdentityUserAccessor.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region BlazorIdentity Pages Templates

    [Fact]
    public void BlazorIdentity_Pages_ConfirmEmailTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ConfirmEmail.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ConfirmEmailChangeTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ConfirmEmailChange.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ExternalLoginTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ExternalLogin.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ForgotPasswordTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ForgotPassword.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ForgotPasswordConfirmationTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ForgotPasswordConfirmation.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_InvalidPasswordResetTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "InvalidPasswordReset.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_InvalidUserTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "InvalidUser.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_LockoutTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Lockout.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_LoginTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Login.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_LoginWith2faTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "LoginWith2fa.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_LoginWithRecoveryCodeTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "LoginWithRecoveryCode.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_RegisterTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Register.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_RegisterConfirmationTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "RegisterConfirmation.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ResendEmailConfirmationTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ResendEmailConfirmation.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ResetPasswordTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ResetPassword.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ResetPasswordConfirmationTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "ResetPasswordConfirmation.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_ImportsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "_Imports.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region BlazorIdentity Pages Manage Templates

    [Fact]
    public void BlazorIdentity_Pages_Manage_ChangePasswordTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "ChangePassword.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_DeletePersonalDataTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "DeletePersonalData.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_Disable2faTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "Disable2fa.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_EmailTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "Email.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_EnableAuthenticatorTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "EnableAuthenticator.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_ExternalLoginsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "ExternalLogins.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_GenerateRecoveryCodesTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "GenerateRecoveryCodes.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_IndexTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "Index.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_PersonalDataTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "PersonalData.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_ResetAuthenticatorTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "ResetAuthenticator.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_SetPasswordTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "SetPassword.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_TwoFactorAuthenticationTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "TwoFactorAuthentication.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_ImportsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "_Imports.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region BlazorIdentity Shared Templates

    [Fact]
    public void BlazorIdentity_Shared_ExternalLoginPickerTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "ExternalLoginPicker.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Shared_ManageLayoutTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "ManageLayout.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Shared_ManageNavMenuTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "ManageNavMenu.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Shared_RedirectToLoginTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "RedirectToLogin.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Shared_ShowRecoveryCodesTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "ShowRecoveryCodes.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Shared_StatusMessageTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "StatusMessage.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region EfController Templates (.cshtml)

    [Fact]
    public void EfController_ApiControllerWithContextTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "EfController", "ApiControllerWithContext.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void EfController_MvcControllerWithContextTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "EfController", "MvcControllerWithContext.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region Files Templates

    [Fact]
    public void Files_IdentityApplicationUserTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "IdentityApplicationUser.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Files_IdentityDbContextTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "IdentityDbContext.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Files_LayoutTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "_Layout.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Files_ErrorTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "Error.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Files_ReadMeTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "ReadMe.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Files_StartupTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "Startup.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region MinimalApi Templates (.cshtml)

    [Fact]
    public void MinimalApi_MinimalApiTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "MinimalApi", "MinimalApi.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void MinimalApi_MinimalApiEfTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "MinimalApi", "MinimalApiEf.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void MinimalApi_MinimalApiEfNoClassTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "MinimalApi", "MinimalApiEfNoClass.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void MinimalApi_MinimalApiNoClassTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "MinimalApi", "MinimalApiNoClass.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region RazorPages Templates

    [Fact]
    public void RazorPages_Bootstrap4_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "Create.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "Create.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_CreatePageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "CreatePageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_CreatePageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "CreatePageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "Delete.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "Delete.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_DeletePageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "DeletePageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_DeletePageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "DeletePageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "Details.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "Details.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_DetailsPageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "DetailsPageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_DetailsPageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "DetailsPageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "Edit.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "Edit.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_EditPageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "EditPageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_EditPageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "EditPageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_ListTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "List.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_ListTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "List.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_ListPageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "ListPageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_ListPageModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "ListPageModel.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap4_ValidationScriptsPartialTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap4", "_ValidationScriptsPartial.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_Bootstrap5_ValidationScriptsPartialTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Bootstrap5", "_ValidationScriptsPartial.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region Views Templates (.cshtml)

    [Fact]
    public void Views_Bootstrap4_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "Create.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "Create.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap4_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "Delete.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "Delete.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap4_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "Details.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "Details.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap4_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "Edit.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "Edit.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap4_EmptyTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "Empty.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_EmptyTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "Empty.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap4_ListTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "List.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_ListTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "List.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap4_ValidationScriptsPartialTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap4", "_ValidationScriptsPartial.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_Bootstrap5_ValidationScriptsPartialTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Bootstrap5", "_ValidationScriptsPartial.cshtml");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion
}
