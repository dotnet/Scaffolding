// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

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
        return Path.Combine(testDir!, "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
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

    [Fact]
    public void BlazorCrud_NotFoundTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorCrud", "NotFound.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region BlazorEntraId Templates

    [Fact]
    public void BlazorEntraId_LoginLogoutEndpointRouteBuilderExtensionsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorEntraId", "LoginLogoutEndpointRouteBuilderExtensions.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorEntraId_LoginOrLogoutTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorEntraId", "LoginOrLogout.tt");
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
    public void BlazorIdentity_PasskeyInputModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "PasskeyInputModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_PasskeyOperationTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "PasskeyOperation.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region BlazorIdentity Pages Templates

    [Fact]
    public void BlazorIdentity_Pages_AccessDeniedTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "AccessDenied.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

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
    public void BlazorIdentity_Pages_Manage_PasskeysTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "Passkeys.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_PersonalDataTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "PersonalData.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_RenamePasskeyTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Pages", "Manage", "RenamePasskey.tt");
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
    public void BlazorIdentity_Shared_PasskeySubmitTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "BlazorIdentity", "Shared", "PasskeySubmit.tt");
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

    #region EfController Templates

    [Fact]
    public void EfController_ApiEfControllerTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "EfController", "ApiEfController.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void EfController_MvcEfControllerTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "EfController", "MvcEfController.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region Files Templates

    [Fact]
    public void Files_ApplicationUserTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Files", "ApplicationUser.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region Identity Templates

    [Fact]
    public void Identity_Pages_ErrorTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Identity", "Pages", "Error.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Identity_Pages_ErrorModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Identity", "Pages", "ErrorModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Identity_Pages_ViewImportsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Identity", "Pages", "_ViewImports.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Identity_Pages_ViewStartTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Identity", "Pages", "_ViewStart.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region MinimalApi Templates

    [Fact]
    public void MinimalApi_MinimalApiTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "MinimalApi", "MinimalApi.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void MinimalApi_MinimalApiEfTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "MinimalApi", "MinimalApiEf.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region RazorPages Templates

    [Fact]
    public void RazorPages_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Create.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_CreateModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "CreateModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Delete.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_DeleteModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "DeleteModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Details.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_DetailsModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "DetailsModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Edit.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_EditModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "EditModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_IndexTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "Index.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void RazorPages_IndexModelTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "RazorPages", "IndexModel.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion

    #region Views Templates

    [Fact]
    public void Views_CreateTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Create.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_DeleteTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Delete.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_DetailsTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Details.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_EditTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Edit.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    [Fact]
    public void Views_IndexTemplate_Exists()
    {
        var templatePath = Path.Combine(GetTemplateBasePath(), "Views", "Index.tt");
        Assert.True(File.Exists(templatePath), $"Template not found: {templatePath}");
    }

    #endregion
}
