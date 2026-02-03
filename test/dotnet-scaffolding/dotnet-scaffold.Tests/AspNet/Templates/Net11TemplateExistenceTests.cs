// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

/// <summary>
/// Tests to verify all required template files exist in the net11.0 folder structure.
/// These tests ensure the TFM-based template organization is maintained correctly.
/// </summary>
public class Net11TemplateExistenceTests
{
    private static string GetTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        // Navigate from artifacts/bin/dotnet-scaffold.Tests/Debug/net11.0 to Templates folder
        // Go up 5 levels to reach the repository root, then down to Templates
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    private static void AssertTemplateExists(string relativePath)
    {
        var basePath = GetTemplatesBasePath();
        // Normalize path separators for cross-platform compatibility
        var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(basePath, normalizedPath);
        Assert.True(File.Exists(fullPath), $"Template file not found: {relativePath}\nBase path: {basePath}\nFull path: {fullPath}");
    }

    private static void AssertTemplateSetExists(string relativePath)
    {
        // Check for .tt, .cs, and .Interfaces.cs files
        AssertTemplateExists($"{relativePath}.tt");
        AssertTemplateExists($"{relativePath}.cs");
        AssertTemplateExists($"{relativePath}.Interfaces.cs");
    }

    #region BlazorCrud Templates

    [Fact]
    public void BlazorCrud_Create_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorCrud\\Create");
    }

    [Fact]
    public void BlazorCrud_Delete_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorCrud\\Delete");
    }

    [Fact]
    public void BlazorCrud_Details_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorCrud\\Details");
    }

    [Fact]
    public void BlazorCrud_Edit_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorCrud\\Edit");
    }

    [Fact]
    public void BlazorCrud_Index_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorCrud\\Index");
    }

    [Fact]
    public void BlazorCrud_NotFound_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorCrud\\NotFound");
    }

    #endregion

    #region BlazorEntraId Templates

    [Fact]
    public void BlazorEntraId_LoginOrLogout_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorEntraId\\LoginOrLogout");
    }

    [Fact]
    public void BlazorEntraId_LoginLogoutEndpointRouteBuilderExtensions_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorEntraId\\LoginLogoutEndpointRouteBuilderExtensions");
    }

    #endregion

    #region BlazorIdentity Templates

    [Fact]
    public void BlazorIdentity_IdentityComponentsEndpointRouteBuilderExtensions_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\IdentityComponentsEndpointRouteBuilderExtensions");
    }

    [Fact]
    public void BlazorIdentity_IdentityNoOpEmailSender_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\IdentityNoOpEmailSender");
    }

    [Fact]
    public void BlazorIdentity_IdentityRedirectManager_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\IdentityRedirectManager");
    }

    [Fact]
    public void BlazorIdentity_IdentityRevalidatingAuthenticationStateProvider_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\IdentityRevalidatingAuthenticationStateProvider");
    }

    [Fact]
    public void BlazorIdentity_PasskeyInputModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\PasskeyInputModel");
    }

    [Fact]
    public void BlazorIdentity_PasskeyOperation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\PasskeyOperation");
    }

    #endregion

    #region BlazorIdentity Pages Templates

    [Fact]
    public void BlazorIdentity_Pages_Imports_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\_Imports");
    }

    [Fact]
    public void BlazorIdentity_Pages_AccessDenied_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\AccessDenied");
    }

    [Fact]
    public void BlazorIdentity_Pages_ConfirmEmail_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ConfirmEmail");
    }

    [Fact]
    public void BlazorIdentity_Pages_ConfirmEmailChange_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ConfirmEmailChange");
    }

    [Fact]
    public void BlazorIdentity_Pages_ExternalLogin_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ExternalLogin");
    }

    [Fact]
    public void BlazorIdentity_Pages_ForgotPassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ForgotPassword");
    }

    [Fact]
    public void BlazorIdentity_Pages_ForgotPasswordConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ForgotPasswordConfirmation");
    }

    [Fact]
    public void BlazorIdentity_Pages_InvalidPasswordReset_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\InvalidPasswordReset");
    }

    [Fact]
    public void BlazorIdentity_Pages_InvalidUser_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\InvalidUser");
    }

    [Fact]
    public void BlazorIdentity_Pages_Lockout_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Lockout");
    }

    [Fact]
    public void BlazorIdentity_Pages_Login_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Login");
    }

    [Fact]
    public void BlazorIdentity_Pages_LoginWith2fa_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\LoginWith2fa");
    }

    [Fact]
    public void BlazorIdentity_Pages_LoginWithRecoveryCode_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\LoginWithRecoveryCode");
    }

    [Fact]
    public void BlazorIdentity_Pages_Register_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Register");
    }

    [Fact]
    public void BlazorIdentity_Pages_RegisterConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\RegisterConfirmation");
    }

    [Fact]
    public void BlazorIdentity_Pages_ResendEmailConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ResendEmailConfirmation");
    }

    [Fact]
    public void BlazorIdentity_Pages_ResetPassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ResetPassword");
    }

    [Fact]
    public void BlazorIdentity_Pages_ResetPasswordConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\ResetPasswordConfirmation");
    }

    #endregion

    #region BlazorIdentity Pages Manage Templates

    [Fact]
    public void BlazorIdentity_Pages_Manage_Imports_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\_Imports");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_ChangePassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\ChangePassword");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_DeletePersonalData_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\DeletePersonalData");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_Disable2fa_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\Disable2fa");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_Email_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\Email");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_EnableAuthenticator_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\EnableAuthenticator");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_ExternalLogins_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\ExternalLogins");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_GenerateRecoveryCodes_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\GenerateRecoveryCodes");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_Index_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\Index");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_Passkeys_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\Passkeys");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_PersonalData_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\PersonalData");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_RenamePasskey_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\RenamePasskey");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_ResetAuthenticator_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\ResetAuthenticator");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_SetPassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\SetPassword");
    }

    [Fact]
    public void BlazorIdentity_Pages_Manage_TwoFactorAuthentication_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Pages\\Manage\\TwoFactorAuthentication");
    }

    #endregion

    #region BlazorIdentity Shared Templates

    [Fact]
    public void BlazorIdentity_Shared_ExternalLoginPicker_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\ExternalLoginPicker");
    }

    [Fact]
    public void BlazorIdentity_Shared_ManageLayout_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\ManageLayout");
    }

    [Fact]
    public void BlazorIdentity_Shared_ManageNavMenu_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\ManageNavMenu");
    }

    [Fact]
    public void BlazorIdentity_Shared_PasskeySubmit_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\PasskeySubmit");
    }

    [Fact]
    public void BlazorIdentity_Shared_RedirectToLogin_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\RedirectToLogin");
    }

    [Fact]
    public void BlazorIdentity_Shared_ShowRecoveryCodes_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\ShowRecoveryCodes");
    }

    [Fact]
    public void BlazorIdentity_Shared_StatusMessage_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\BlazorIdentity\\Shared\\StatusMessage");
    }

    #endregion

    #region EfController Templates

    [Fact]
    public void EfController_ApiEfController_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\EfController\\ApiEfController");
    }

    [Fact]
    public void EfController_MvcEfController_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\EfController\\MvcEfController");
    }

    #endregion

    #region Files Templates

    [Fact]
    public void Files_ApplicationUser_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Files\\ApplicationUser");
    }

    #endregion

    #region Identity Templates

    [Fact]
    public void Identity_Pages_ViewImports_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\_ViewImports");
    }

    [Fact]
    public void Identity_Pages_ViewStart_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\_ViewStart");
    }

    [Fact]
    public void Identity_Pages_Error_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Error");
    }

    [Fact]
    public void Identity_Pages_ErrorModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\ErrorModel");
    }

    [Fact]
    public void Identity_Pages_Account_ViewImports_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\_ViewImports");
    }

    [Fact]
    public void Identity_Pages_Account_StatusMessage_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\_StatusMessage");
    }

    [Fact]
    public void Identity_Pages_Account_AccessDenied_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\AccessDenied");
    }

    [Fact]
    public void Identity_Pages_Account_AccessDeniedModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\AccessDeniedModel");
    }

    [Fact]
    public void Identity_Pages_Account_ConfirmEmail_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ConfirmEmail");
    }

    [Fact]
    public void Identity_Pages_Account_ConfirmEmailModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ConfirmEmailModel");
    }

    [Fact]
    public void Identity_Pages_Account_ConfirmEmailChange_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ConfirmEmailChange");
    }

    [Fact]
    public void Identity_Pages_Account_ConfirmEmailChangeModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ConfirmEmailChangeModel");
    }

    [Fact]
    public void Identity_Pages_Account_ExternalLogin_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ExternalLogin");
    }

    [Fact]
    public void Identity_Pages_Account_ExternalLoginModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ExternalLoginModel");
    }

    [Fact]
    public void Identity_Pages_Account_ForgotPassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ForgotPassword");
    }

    [Fact]
    public void Identity_Pages_Account_ForgotPasswordModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ForgotPasswordModel");
    }

    [Fact]
    public void Identity_Pages_Account_ForgotPasswordConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ForgotPasswordConfirmation");
    }

    [Fact]
    public void Identity_Pages_Account_ForgotPasswordConfirmationModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ForgotPasswordConfirmationModel");
    }

    [Fact]
    public void Identity_Pages_Account_Lockout_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Lockout");
    }

    [Fact]
    public void Identity_Pages_Account_LockoutModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LockoutModel");
    }

    [Fact]
    public void Identity_Pages_Account_Login_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Login");
    }

    [Fact]
    public void Identity_Pages_Account_LoginModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LoginModel");
    }

    [Fact]
    public void Identity_Pages_Account_LoginWith2fa_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LoginWith2fa");
    }

    [Fact]
    public void Identity_Pages_Account_LoginWith2faModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LoginWith2faModel");
    }

    [Fact]
    public void Identity_Pages_Account_LoginWithRecoveryCode_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LoginWithRecoveryCode");
    }

    [Fact]
    public void Identity_Pages_Account_LoginWithRecoveryCodeModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LoginWithRecoveryCodeModel");
    }

    [Fact]
    public void Identity_Pages_Account_Logout_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Logout");
    }

    [Fact]
    public void Identity_Pages_Account_LogoutModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\LogoutModel");
    }

    [Fact]
    public void Identity_Pages_Account_Register_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Register");
    }

    [Fact]
    public void Identity_Pages_Account_RegisterModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\RegisterModel");
    }

    [Fact]
    public void Identity_Pages_Account_RegisterConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\RegisterConfirmation");
    }

    [Fact]
    public void Identity_Pages_Account_RegisterConfirmationModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\RegisterConfirmationModel");
    }

    [Fact]
    public void Identity_Pages_Account_ResendEmailConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ResendEmailConfirmation");
    }

    [Fact]
    public void Identity_Pages_Account_ResendEmailConfirmationModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ResendEmailConfirmationModel");
    }

    [Fact]
    public void Identity_Pages_Account_ResetPassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ResetPassword");
    }

    [Fact]
    public void Identity_Pages_Account_ResetPasswordModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ResetPasswordModel");
    }

    [Fact]
    public void Identity_Pages_Account_ResetPasswordConfirmation_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ResetPasswordConfirmation");
    }

    [Fact]
    public void Identity_Pages_Account_ResetPasswordConfirmationModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\ResetPasswordConfirmationModel");
    }

    #endregion

    #region Identity Pages Account Manage Templates

    [Fact]
    public void Identity_Pages_Account_Manage_ViewImports_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\_ViewImports");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_Layout_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\_Layout");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ManageNav_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\_ManageNav");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_StatusMessage_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\_StatusMessage");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ChangePassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ChangePassword");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ChangePasswordModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ChangePasswordModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_DeletePersonalData_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\DeletePersonalData");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_DeletePersonalDataModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\DeletePersonalDataModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_Disable2fa_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\Disable2fa");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_Disable2faModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\Disable2faModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_DownloadPersonalData_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\DownloadPersonalData");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_DownloadPersonalDataModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\DownloadPersonalDataModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_Email_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\Email");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_EmailModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\EmailModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_EnableAuthenticator_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\EnableAuthenticator");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_EnableAuthenticatorModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\EnableAuthenticatorModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ExternalLogins_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ExternalLogins");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ExternalLoginsModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ExternalLoginsModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_GenerateRecoveryCodes_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\GenerateRecoveryCodes");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_GenerateRecoveryCodesModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\GenerateRecoveryCodesModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_Index_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\Index");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_IndexModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\IndexModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ManageNavPagesModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ManageNavPagesModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_PersonalData_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\PersonalData");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_PersonalDataModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\PersonalDataModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ResetAuthenticator_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ResetAuthenticator");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ResetAuthenticatorModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ResetAuthenticatorModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_SetPassword_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\SetPassword");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_SetPasswordModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\SetPasswordModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ShowRecoveryCodes_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ShowRecoveryCodes");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_ShowRecoveryCodesModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\ShowRecoveryCodesModel");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_TwoFactorAuthentication_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\TwoFactorAuthentication");
    }

    [Fact]
    public void Identity_Pages_Account_Manage_TwoFactorAuthenticationModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Identity\\Pages\\Account\\Manage\\TwoFactorAuthenticationModel");
    }

    #endregion

    #region MinimalApi Templates

    [Fact]
    public void MinimalApi_MinimalApi_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\MinimalApi\\MinimalApi");
    }

    [Fact]
    public void MinimalApi_MinimalApiEf_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\MinimalApi\\MinimalApiEf");
    }

    #endregion

    #region RazorPages Templates

    [Fact]
    public void RazorPages_Create_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\Create");
    }

    [Fact]
    public void RazorPages_CreateModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\CreateModel");
    }

    [Fact]
    public void RazorPages_Delete_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\Delete");
    }

    [Fact]
    public void RazorPages_DeleteModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\DeleteModel");
    }

    [Fact]
    public void RazorPages_Details_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\Details");
    }

    [Fact]
    public void RazorPages_DetailsModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\DetailsModel");
    }

    [Fact]
    public void RazorPages_Edit_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\Edit");
    }

    [Fact]
    public void RazorPages_EditModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\EditModel");
    }

    [Fact]
    public void RazorPages_Index_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\Index");
    }

    [Fact]
    public void RazorPages_IndexModel_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\RazorPages\\IndexModel");
    }

    #endregion

    #region Views Templates

    [Fact]
    public void Views_Create_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Views\\Create");
    }

    [Fact]
    public void Views_Delete_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Views\\Delete");
    }

    [Fact]
    public void Views_Details_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Views\\Details");
    }

    [Fact]
    public void Views_Edit_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Views\\Edit");
    }

    [Fact]
    public void Views_Index_TemplateExists()
    {
        AssertTemplateSetExists("net11.0\\Views\\Index");
    }

    #endregion
}
