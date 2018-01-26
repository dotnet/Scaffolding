// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public static class IdentityGeneratorFilesConfig
    {
        public static string[] GetAreaFolders(bool isDataRequired)
        {
            return isDataRequired
                ? new string[]
                {
                    "Data",
                    "Pages",
                    "Services"
                }
                : new string[]
                {
                    "Pages",
                    "Services"
                };
        }

        public static Dictionary<string, string> Templates = new Dictionary<string,string>()
        {
            {"EmailSender.cshtml", Path.Combine("Areas", "Identity","Services", "EmailSender.cs")},
            {"IEmailSender.cshtml", Path.Combine("Areas", "Identity","Services", "IEmailSender.cs")},
            {"_Layout.cshtml", Path.Combine("Areas", "Identity", "Pages", "_Layout.cshtml")},
            {"_ViewImports.cshtml", Path.Combine("Areas", "Identity", "Pages", "_ViewImports.cshtml")},
            {"Error.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Error.cshtml.cs")},

            // Accounts
            {"Account._ViewImports.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "_ViewImports.cshtml")},
            {"Account.AccessDenied.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "AccessDenied.cshtml.cs")},
            {"Account.ConfirmEmail.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ConfirmEmail.cshtml.cs")},
            {"Account.ExternalLogin.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs")},
            {"Account.ForgotPassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPassword.cshtml.cs")},
            {"Account.ForgotPasswordConfirmation.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPasswordConfirmation.cshtml.cs")},
            {"Account.Lockout.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Lockout.cshtml.cs")},
            {"Account.Login.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Login.cshtml.cs")},
            {"Account.LoginWith2fa.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWith2fa.cshtml.cs")},
            {"Account.LoginWithRecoveryCode.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWithRecoveryCode.cshtml.cs")},
            {"Account.Register.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs")},
            {"Account.ResetPassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml.cs")},
            {"Account.ResetPasswordConfirmation.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPasswordConfirmation.cshtml.cs")},
            {"Account.Logout.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Logout.cshtml.cs")},
            
            // Accounts/Manage
            {"Account.Manage._ViewImports.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_ViewImports.cshtml")},
            {"Account.Manage.ChangePassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml.cs")},
            {"Account.Manage.DeletePersonalData.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DeletePersonalData.cshtml.cs")},
            {"Account.Manage.DownloadPersonalData.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DownloadPersonalData.cshtml.cs")},
            {"Account.Manage.Disable2fa.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Disable2fa.cshtml.cs")},
            {"Account.Manage.EnableAuthenticator.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "EnableAuthenticator.cshtml.cs")},
            {"Account.Manage.ExternalLogins.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml.cs")},
            {"Account.Manage.GenerateRecoveryCodes.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "GenerateRecoveryCodes.cshtml.cs")},
            {"Account.Manage.Index.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml.cs")},
            {"Account.Manage.PersonalData.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "PersonalData.cshtml.cs")},
            {"Account.Manage.ResetAuthenticator.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ResetAuthenticator.cshtml.cs")},
            {"Account.Manage.SetPassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml.cs")},
            {"Account.Manage.TwoFactorAuthentication.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "TwoFactorAuthentication.cshtml.cs")},
            {"Account.Manage.ManageNavPages.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ManageNavPages.cs")},
            {"Account.Manage._ManageNav.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_ManageNav.cshtml")},

            //IdentityHostingStartup
            {"IdentityHostingStartup.cshtml", Path.Combine("Areas", "Identity", "IdentityHostingStartup.cs")},
            // LoginPartial
            {"_LoginPartial.cshtml", Path.Combine("Pages", "Shared", "_LoginPartial.cshtml")}
        };

        public static Dictionary<string, string> StaticFiles = new Dictionary<string, string>()
        {
            {Path.Combine("Pages", "_ValidationScriptsPartial.cshtml"), Path.Combine("Areas", "Identity", "Pages", "_ValidationScriptsPartial.cshtml")},
            {Path.Combine("Pages", "_ViewStart.cshtml"), Path.Combine("Areas", "Identity", "Pages", "_ViewStart.cshtml")},
            {Path.Combine("Pages", "Error.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Error.cshtml")},

            // Accounts
            {Path.Combine("Pages", "Account", "Account.AccessDenied.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "AccessDenied.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ConfirmEmail.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ConfirmEmail.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ExternalLogin.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ForgotPassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPassword.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ForgotPasswordConfirmation.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPasswordConfirmation.cshtml")},
            {Path.Combine("Pages", "Account", "Account.Lockout.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Lockout.cshtml")},
            {Path.Combine("Pages", "Account", "Account.Login.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Login.cshtml")},
            {Path.Combine("Pages", "Account", "Account.LoginWith2fa.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWith2fa.cshtml")},
            {Path.Combine("Pages", "Account", "Account.LoginWithRecoveryCode.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWithRecoveryCode.cshtml")},
            {Path.Combine("Pages", "Account", "Account.Register.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Register.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ResetPassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ResetPasswordConfirmation.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPasswordConfirmation.cshtml")},
            {Path.Combine("Pages", "Account", "Account.Logout.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Logout.cshtml")},

            // Accounts/Manage
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage._Layout.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_Layout.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage._StatusMessage.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_StatusMessage.cshtml")},
            

            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.ChangePassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.DeletePersonalData.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DeletePersonalData.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.DownloadPersonalData.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "DownloadPersonalData.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.Disable2fa.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Disable2fa.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.EnableAuthenticator.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "EnableAuthenticator.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.ExternalLogins.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.GenerateRecoveryCodes.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "GenerateRecoveryCodes.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.Index.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.PersonalData.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "PersonalData.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.ResetAuthenticator.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ResetAuthenticator.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.SetPassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.TwoFactorAuthentication.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "TwoFactorAuthentication.cshtml")},

            {Path.Combine("wwwroot","css","site.css"), Path.Combine("wwwroot","identity","css","site.css")},
            {Path.Combine("wwwroot","css","site.min.css"), Path.Combine("wwwroot","identity","css","site.min.css")},
            {Path.Combine("wwwroot","js","site.js"), Path.Combine("wwwroot","identity","js","site.js")},
            {Path.Combine("wwwroot","js","site.min.js"), Path.Combine("wwwroot","identity","js","site.min.js")},
            {Path.Combine("wwwroot","lib","bootstrap",".bower.json"), Path.Combine("wwwroot","identity","lib","bootstrap", ".bower.json")},
            {Path.Combine("wwwroot","lib","bootstrap","LICENSE"), Path.Combine("wwwroot","identity","lib","bootstrap","LICENSE")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.css"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.css")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.min.css"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.min.css")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.min.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.min.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.css"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.css")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.min.css"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.min.css")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.min.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.min.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.eot"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.eot")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.svg"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.svg")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.ttf"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.ttf")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff2"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff2")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","js","bootstrap.js"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","bootstrap.js")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","js","bootstrap.min.js"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","bootstrap.min.js")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","js","npm.js"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","npm.js")},
            {Path.Combine("wwwroot","lib","jquery", ".bower.json"), Path.Combine("wwwroot","identity","lib","jquery",".bower.json")},
            {Path.Combine("wwwroot","lib","jquery","LICENSE.txt"), Path.Combine("wwwroot","identity","lib","jquery","LICENSE.txt")},
            {Path.Combine("wwwroot","lib","jquery", "dist", "jquery.js"), Path.Combine("wwwroot","identity","lib","jquery","dist","jquery.js")},
            {Path.Combine("wwwroot","lib","jquery", "dist", "jquery.min.js"), Path.Combine("wwwroot","identity","lib","jquery","dist","jquery.min.js")},
            {Path.Combine("wwwroot","lib","jquery", "dist", "jquery.min.map"), Path.Combine("wwwroot","identity","lib","jquery","dist","jquery.min.map")},
            {Path.Combine("wwwroot","lib","jquery-validation","LICENSE.md"), Path.Combine("wwwroot","identity","lib","jquery-validation","LICENSE.md")},
            {Path.Combine("wwwroot","lib","jquery-validation",".bower.json"), Path.Combine("wwwroot","identity","lib","jquery-validation",".bower.json")},
            {Path.Combine("wwwroot","lib","jquery-validation","dist","additional-methods.js"), Path.Combine("wwwroot","identity","lib","jquery-validation","dist","additional-methods.js")},
            {Path.Combine("wwwroot","lib","jquery-validation","dist","additional-methods.min.js"), Path.Combine("wwwroot","identity","lib","jquery-validation","dist","additional-methods.min.js")},
            {Path.Combine("wwwroot","lib","jquery-validation","dist","jquery.validate.js"), Path.Combine("wwwroot","identity","lib","jquery-validation","dist","jquery.validate.js")},
            {Path.Combine("wwwroot","lib","jquery-validation","dist","jquery.validate.min.js"), Path.Combine("wwwroot","identity","lib","jquery-validation","dist","jquery.validate.min.js")},
            {Path.Combine("wwwroot","lib","jquery-validation-unobtrusive",".bower.json"), Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive",".bower.json")},
            {Path.Combine("wwwroot","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.js"), Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.js")},
            {Path.Combine("wwwroot","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.min.js"), Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.min.js")},
            {"ScaffoldingReadme.txt", Path.Combine(".","ScaffoldingReadme.txt")}
        };

        public static Dictionary<string, string> GetTemplateFiles(IdentityGeneratorTemplateModel templateModel)
        {
            if (templateModel == null)
            {
                throw new ArgumentNullException(nameof(templateModel));
            }

            var templates = new Dictionary<string, string>(Templates);

            if (!templateModel.IsUsingExistingDbContext)
            {
                templates.Add("ApplicationDbContext.cshtml", Path.Combine("Areas", "Identity", "Data", $"{templateModel.DbContextClass}.cs"));

                if (templateModel.IsGenerateCustomUser)
                {
                    templates.Add("ApplicationUser.cshtml", Path.Combine("Areas", "Identity", "Data", $"{templateModel.UserClass}.cs"));
                }
            }

            return templates;
        }
    }
}