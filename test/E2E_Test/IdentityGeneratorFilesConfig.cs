// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public static class IdentityGeneratorFilesConfig
    {
        public static List<string> Templates = new List<string>()
        {
            Path.Combine("Areas", "Identity","Services", "EmailSender.cs"),
            Path.Combine("Areas", "Identity","Services", "IEmailSender.cs"),
            Path.Combine("Areas", "Identity", "Pages", "_Layout.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "_ViewImports.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Error.cshtml.cs"),

            // Accounts
            Path.Combine("Areas", "Identity", "Pages", "Account", "_ViewImports.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "AccessDenied.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ConfirmEmail.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPassword.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPasswordConfirmation.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Lockout.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Login.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWith2fa.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWithRecoveryCode.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPasswordConfirmation.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Logout.cshtml.cs"),
            
            // Accounts/Manage
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_ViewImports.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DeletePersonalData.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DownloadPersonalData.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Disable2fa.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "EnableAuthenticator.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "GenerateRecoveryCodes.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "PersonalData.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ResetAuthenticator.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "TwoFactorAuthentication.cshtml.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ManageNavPages.cs"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_ManageNav.cshtml"),

            //IdentityHostingStartup
            Path.Combine("Areas", "Identity", "IdentityHostingStartup.cs"),
            // LoginPartial
            Path.Combine("Pages", "Shared", "_LoginPartial.cshtml")
        };

        public static List<string> StaticFiles = new List<string>()
        {
            Path.Combine("Areas", "Identity", "Pages", "_ValidationScriptsPartial.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "_ViewStart.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Error.cshtml"),

            // Accounts
            Path.Combine("Areas", "Identity", "Pages", "Account", "AccessDenied.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ConfirmEmail.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPassword.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPasswordConfirmation.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Lockout.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Login.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWith2fa.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWithRecoveryCode.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Register.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPasswordConfirmation.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Logout.cshtml"),

            // Accounts/Manage
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_Layout.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_StatusMessage.cshtml"),
            

            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DeletePersonalData.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DownloadPersonalData.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Disable2fa.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "EnableAuthenticator.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "GenerateRecoveryCodes.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "PersonalData.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ResetAuthenticator.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml"),
            Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "TwoFactorAuthentication.cshtml"),

            Path.Combine("wwwroot","identity","css","site.css"),
            Path.Combine("wwwroot","identity","css","site.min.css"),
            Path.Combine("wwwroot","identity","js","site.js"),
            Path.Combine("wwwroot","identity","js","site.min.js"),
            Path.Combine("wwwroot","identity","lib","bootstrap", ".bower.json"),
            Path.Combine("wwwroot","identity","lib","bootstrap","LICENSE"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.css"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.min.css"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.css.map"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.min.css.map"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.css"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.min.css"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.css.map"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.min.css.map"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.eot"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.svg"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.ttf"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff2"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","bootstrap.js"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","bootstrap.min.js"),
            Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","npm.js"),
            Path.Combine("wwwroot","identity","lib","jquery",".bower.json"),
            Path.Combine("wwwroot","identity","lib","jquery","LICENSE.txt"),
            Path.Combine("wwwroot","identity","lib","jquery","dist","jquery.js"),
            Path.Combine("wwwroot","identity","lib","jquery","dist","jquery.min.js"),
            Path.Combine("wwwroot","identity","lib","jquery","dist","jquery.min.map"),
            Path.Combine("wwwroot","identity","lib","jquery-validation","LICENSE.md"),
            Path.Combine("wwwroot","identity","lib","jquery-validation",".bower.json"),
            Path.Combine("wwwroot","identity","lib","jquery-validation","dist","additional-methods.js"),
            Path.Combine("wwwroot","identity","lib","jquery-validation","dist","additional-methods.min.js"),
            Path.Combine("wwwroot","identity","lib","jquery-validation","dist","jquery.validate.js"),
            Path.Combine("wwwroot","identity","lib","jquery-validation","dist","jquery.validate.min.js"),
            Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive",".bower.json"),
            Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.js"),
            Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.min.js"),
            "ScaffoldingReadme.txt"
        };
    }
}