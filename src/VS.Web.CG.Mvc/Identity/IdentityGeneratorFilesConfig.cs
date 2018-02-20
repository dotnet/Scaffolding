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

        public static Dictionary<string, string> Templates = new Dictionary<string, string>()
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

            var temp = _config.NamedFileConfig.SelectMany(e => e.Value.Where(t => t.IsTemplate));

            var templates = new Dictionary<string, string>();

            foreach(var t in temp)
            {
                templates.Add(t.SourcePath, t.OutputPath);
            }

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

        private static IdentityGeneratorFiles _config;

        static IdentityGeneratorFilesConfig()
        {
            _config = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentityGeneratorFiles>(configStr);
        }

        public static IEnumerable<string> GetFilesToList()
        {
            return _config.NamedFileConfig
                .Where(c => c.Value.Any(f => f.ShowInListFiles))
                .Select(c => c.Key);
        }

        private static string configStr = @"
{
  ""NamedFileConfig"": {
    ""Content"": [
      {
        ""Name"": "".bower"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/.bower.json"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/.bower.json"",
        ""IsTemplate"": false
      },
      {
        ""Name"": "".bower"",
        ""SourcePath"": ""wwwroot/lib/jquery/.bower.json"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery/.bower.json"",
        ""IsTemplate"": false
      },
      {
        ""Name"": "".bower"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation/.bower.json"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation/.bower.json"",
        ""IsTemplate"": false
      },
      {
        ""Name"": "".bower"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation-unobtrusive/.bower.json"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation-unobtrusive/.bower.json"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""additional-methods"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation/dist/additional-methods.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation/dist/additional-methods.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""additional-methods.min"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation/dist/additional-methods.min.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation/dist/additional-methods.min.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap.css"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap.css"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/js/bootstrap.js"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/js/bootstrap.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap.css"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap.css.map"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap.css.map"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap.min"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap.min.css"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap.min.css"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap.min"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/js/bootstrap.min.js"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/js/bootstrap.min.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap.min.css"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap.min.css.map"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap.min.css.map"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap-theme"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap-theme.css"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap-theme.css"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap-theme.css"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap-theme.css.map"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap-theme.css.map"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap-theme.min"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap-theme.min.css"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap-theme.min.css"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""bootstrap-theme.min.css"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/css/bootstrap-theme.min.css.map"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/css/bootstrap-theme.min.css.map"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""glyphicons-halflings-regular"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.eot"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.eot"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""glyphicons-halflings-regular"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.svg"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.svg"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""glyphicons-halflings-regular"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.ttf"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.ttf"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""glyphicons-halflings-regular"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.woff"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.woff"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""glyphicons-halflings-regular"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.woff2"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.woff2"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery"",
        ""SourcePath"": ""wwwroot/lib/jquery/dist/jquery.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery/dist/jquery.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery.min"",
        ""SourcePath"": ""wwwroot/lib/jquery/dist/jquery.min.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery/dist/jquery.min.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery.min"",
        ""SourcePath"": ""wwwroot/lib/jquery/dist/jquery.min.map"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery/dist/jquery.min.map"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery.validate"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation/dist/jquery.validate.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation/dist/jquery.validate.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery.validate.min"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation/dist/jquery.validate.min.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation/dist/jquery.validate.min.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery.validate.unobtrusive"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""jquery.validate.unobtrusive.min"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""LICENSE"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/LICENSE"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/LICENSE"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""LICENSE"",
        ""SourcePath"": ""wwwroot/lib/jquery/LICENSE.txt"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery/LICENSE.txt"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""LICENSE"",
        ""SourcePath"": ""wwwroot/lib/jquery-validation/LICENSE.md"",
        ""OutputPath"": ""wwwroot/identity/lib/jquery-validation/LICENSE.md"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""npm"",
        ""SourcePath"": ""wwwroot/lib/bootstrap/dist/js/npm.js"",
        ""OutputPath"": ""wwwroot/identity/lib/bootstrap/dist/js/npm.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""site"",
        ""SourcePath"": ""wwwroot/css/site.css"",
        ""OutputPath"": ""wwwroot/identity/css/site.css"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""site"",
        ""SourcePath"": ""wwwroot/js/site.js"",
        ""OutputPath"": ""wwwroot/identity/js/site.js"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""site.min"",
        ""SourcePath"": ""wwwroot/css/site.min.css"",
        ""OutputPath"": ""wwwroot/identity/css/site.min.css"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""site.min"",
        ""SourcePath"": ""wwwroot/js/site.min.js"",
        ""OutputPath"": ""wwwroot/identity/js/site.min.js"",
        ""IsTemplate"": false
      }
    ],
    ""_Layout"": [
      {
        ""Name"": ""_Layout"",
        ""SourcePath"": ""_Layout.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/_Layout.cshtml"",
        ""IsTemplate"": true
      }
    ],
    ""_LoginPartial"": [
      {
        ""Name"": ""_LoginPartial"",
        ""SourcePath"": ""_LoginPartial.cshtml"",
        ""OutputPath"": ""Pages/Shared/_LoginPartial.cshtml"",
        ""IsTemplate"": true
      }
    ],
    ""_ValidationScriptsPartial"": [
      {
        ""Name"": ""_ValidationScriptsPartial"",
        ""SourcePath"": ""Pages/_ValidationScriptsPartial.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/_ValidationScriptsPartial.cshtml"",
        ""IsTemplate"": false
      }
    ],
    ""_ViewImports"": [
      {
        ""Name"": ""_ViewImports"",
        ""SourcePath"": ""_ViewImports.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/_ViewImports.cshtml"",
        ""IsTemplate"": true
      }
    ],
    ""_ViewStart"": [
      {
        ""Name"": ""_ViewStart"",
        ""SourcePath"": ""Pages/_ViewStart.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/_ViewStart.cshtml"",
        ""IsTemplate"": false
      }
    ],
    ""Account._ViewImports"": [
      {
        ""Name"": ""Account._ViewImports"",
        ""SourcePath"": ""Account._ViewImports.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/_ViewImports.cshtml"",
        ""IsTemplate"": true
      }
    ],
    ""Account.AccessDenied"": [
      {
        ""Name"": ""Account.AccessDenied"",
        ""SourcePath"": ""Pages/Account/Account.AccessDenied.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/AccessDenied.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.AccessDenied.cs"",
        ""SourcePath"": ""Account.AccessDenied.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/AccessDenied.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.ConfirmEmail"": [
      {
        ""Name"": ""Account.ConfirmEmail"",
        ""SourcePath"": ""Pages/Account/Account.ConfirmEmail.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ConfirmEmail.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.ConfirmEmail.cs"",
        ""SourcePath"": ""Account.ConfirmEmail.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ConfirmEmail.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.ExternalLogin"": [
      {
        ""Name"": ""Account.ExternalLogin"",
        ""SourcePath"": ""Pages/Account/Account.ExternalLogin.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ExternalLogin.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.ExternalLogin.cs"",
        ""SourcePath"": ""Account.ExternalLogin.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.ForgotPassword"": [
      {
        ""Name"": ""Account.ForgotPassword"",
        ""SourcePath"": ""Pages/Account/Account.ForgotPassword.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ForgotPassword.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.ForgotPassword.cs"",
        ""SourcePath"": ""Account.ForgotPassword.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.ForgotPasswordConfirmation"": [
      {
        ""Name"": ""Account.ForgotPasswordConfirmation"",
        ""SourcePath"": ""Pages/Account/Account.ForgotPasswordConfirmation.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.ForgotPasswordConfirmation.cs"",
        ""SourcePath"": ""Account.ForgotPasswordConfirmation.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Lockout"": [
      {
        ""Name"": ""Account.Lockout"",
        ""SourcePath"": ""Pages/Account/Account.Lockout.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Lockout.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Lockout.cs"",
        ""SourcePath"": ""Account.Lockout.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Lockout.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Login"": [
      {
        ""Name"": ""Account.Login"",
        ""SourcePath"": ""Pages/Account/Account.Login.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Login.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Login.cs"",
        ""SourcePath"": ""Account.Login.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Login.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.LoginWith2fa"": [
      {
        ""Name"": ""Account.LoginWith2fa"",
        ""SourcePath"": ""Pages/Account/Account.LoginWith2fa.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/LoginWith2fa.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.LoginWith2fa.cs"",
        ""SourcePath"": ""Account.LoginWith2fa.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/LoginWith2fa.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.LoginWithRecoveryCode"": [
      {
        ""Name"": ""Account.LoginWithRecoveryCode"",
        ""SourcePath"": ""Pages/Account/Account.LoginWithRecoveryCode.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/LoginWithRecoveryCode.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.LoginWithRecoveryCode.cs"",
        ""SourcePath"": ""Account.LoginWithRecoveryCode.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/LoginWithRecoveryCode.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Logout"": [
      {
        ""Name"": ""Account.Logout"",
        ""SourcePath"": ""Pages/Account/Account.Logout.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Logout.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Logout.cs"",
        ""SourcePath"": ""Account.Logout.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Logout.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage._Layout"": [
      {
        ""Name"": ""Account.Manage._Layout"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage._Layout.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/_Layout.cshtml"",
        ""IsTemplate"": false
      }
    ],
    ""Account.Manage._ManageNav"": [
      {
        ""Name"": ""Account.Manage._ManageNav"",
        ""SourcePath"": ""Account.Manage._ManageNav.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/_ManageNav.cshtml"",
        ""IsTemplate"": true
      },
      {
        ""Name"": ""Account.Manage.ManageNavPages"",
        ""SourcePath"": ""Account.Manage.ManageNavPages.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ManageNavPages.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage._StatusMessage"": [
      {
        ""Name"": ""Account.Manage._StatusMessage"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage._StatusMessage.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/_StatusMessage.cshtml"",
        ""IsTemplate"": false
      }
    ],
    ""Account.Manage._ViewImports"": [
      {
        ""Name"": ""Account.Manage._ViewImports"",
        ""SourcePath"": ""Account.Manage._ViewImports.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/_ViewImports.cshtml"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.ChangePassword"": [
      {
        ""Name"": ""Account.Manage.ChangePassword"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.ChangePassword.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.ChangePassword.cs"",
        ""SourcePath"": ""Account.Manage.ChangePassword.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.DeletePersonalData"": [
      {
        ""Name"": ""Account.Manage.DeletePersonalData"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.DeletePersonalData.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.DeletePersonalData.cs"",
        ""SourcePath"": ""Account.Manage.DeletePersonalData.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.Disable2fa"": [
      {
        ""Name"": ""Account.Manage.Disable2fa"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.Disable2fa.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/Disable2fa.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.Disable2fa.cs"",
        ""SourcePath"": ""Account.Manage.Disable2fa.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/Disable2fa.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.DownloadPersonalData"": [
      {
        ""Name"": ""Account.Manage.DownloadPersonalData"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.DownloadPersonalData.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/DownloadPersonalData.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.DownloadPersonalData.cs"",
        ""SourcePath"": ""Account.Manage.DownloadPersonalData.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/DownloadPersonalData.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.EnableAuthenticator"": [
      {
        ""Name"": ""Account.Manage.EnableAuthenticator"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.EnableAuthenticator.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/EnableAuthenticator.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.EnableAuthenticator.cs"",
        ""SourcePath"": ""Account.Manage.EnableAuthenticator.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/EnableAuthenticator.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.ExternalLogins"": [
      {
        ""Name"": ""Account.Manage.ExternalLogins"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.ExternalLogins.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ExternalLogins.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.ExternalLogins.cs"",
        ""SourcePath"": ""Account.Manage.ExternalLogins.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ExternalLogins.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.GenerateRecoveryCodes"": [
      {
        ""Name"": ""Account.Manage.GenerateRecoveryCodes"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.GenerateRecoveryCodes.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/GenerateRecoveryCodes.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.GenerateRecoveryCodes.cs"",
        ""SourcePath"": ""Account.Manage.GenerateRecoveryCodes.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/GenerateRecoveryCodes.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.Index"": [
      {
        ""Name"": ""Account.Manage.Index"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.Index.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/Index.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.Index.cs"",
        ""SourcePath"": ""Account.Manage.Index.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/Index.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.PersonalData"": [
      {
        ""Name"": ""Account.Manage.PersonalData"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.PersonalData.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/PersonalData.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.PersonalData.cs"",
        ""SourcePath"": ""Account.Manage.PersonalData.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/PersonalData.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.ResetAuthenticator"": [
      {
        ""Name"": ""Account.Manage.ResetAuthenticator"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.ResetAuthenticator.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ResetAuthenticator.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.ResetAuthenticator.cs"",
        ""SourcePath"": ""Account.Manage.ResetAuthenticator.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/ResetAuthenticator.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.SetPassword"": [
      {
        ""Name"": ""Account.Manage.SetPassword"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.SetPassword.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/SetPassword.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.SetPassword.cs"",
        ""SourcePath"": ""Account.Manage.SetPassword.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/SetPassword.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Manage.TwoFactorAuthentication"": [
      {
        ""Name"": ""Account.Manage.TwoFactorAuthentication"",
        ""SourcePath"": ""Pages/Account/Manage/Account.Manage.TwoFactorAuthentication.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/TwoFactorAuthentication.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Manage.TwoFactorAuthentication.cs"",
        ""SourcePath"": ""Account.Manage.TwoFactorAuthentication.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Manage/TwoFactorAuthentication.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.Register"": [
      {
        ""Name"": ""Account.Register"",
        ""SourcePath"": ""Pages/Account/Account.Register.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Register.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.Register.cs"",
        ""SourcePath"": ""Account.Register.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/Register.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.ResetPassword"": [
      {
        ""Name"": ""Account.ResetPassword"",
        ""SourcePath"": ""Pages/Account/Account.ResetPassword.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ResetPassword.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.ResetPassword.cs"",
        ""SourcePath"": ""Account.ResetPassword.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ResetPassword.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""Account.ResetPasswordConfirmation"": [
      {
        ""Name"": ""Account.ResetPasswordConfirmation"",
        ""SourcePath"": ""Pages/Account/Account.ResetPasswordConfirmation.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ResetPasswordConfirmation.cshtml"",
        ""IsTemplate"": false
      },
      {
        ""Name"": ""Account.ResetPasswordConfirmation.cs"",
        ""SourcePath"": ""Account.ResetPasswordConfirmation.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Account/ResetPasswordConfirmation.cshtml.cs"",
        ""IsTemplate"": true
      }
    ],
    ""EmailSender"": [
      {
        ""Name"": ""EmailSender"",
        ""SourcePath"": ""EmailSender.cshtml"",
        ""OutputPath"": ""Areas/Identity/Services/EmailSender.cs"",
        ""IsTemplate"": true,
        ""ShowInListFiles"": false
      },
      {
        ""Name"": ""IEmailSender"",
        ""SourcePath"": ""IEmailSender.cshtml"",
        ""OutputPath"": ""Areas/Identity/Services/IEmailSender.cs"",
        ""IsTemplate"": true,
        ""ShowInListFiles"": false
      }
    ],
    ""Error"": [
      {
        ""Name"": ""Error"",
        ""SourcePath"": ""Pages/Error.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Error.cshtml"",
        ""IsTemplate"": false,
        ""ShowInListFiles"": false
      },
      {
        ""Name"": ""Error.cs"",
        ""SourcePath"": ""Error.cs.cshtml"",
        ""OutputPath"": ""Areas/Identity/Pages/Error.cshtml.cs"",
        ""IsTemplate"": true,
        ""ShowInListFiles"": false
      }
    ],
    ""IdentityHostingStartup"": [
      {
        ""Name"": ""IdentityHostingStartup"",
        ""SourcePath"": ""IdentityHostingStartup.cshtml"",
        ""OutputPath"": ""Areas/Identity/IdentityHostingStartup.cs"",
        ""IsTemplate"": true,
        ""ShowInListFiles"": false
      }
    ],
    ""ScaffoldingReadme"": [
      {
        ""Name"": ""ScaffoldingReadme"",
        ""SourcePath"": ""ScaffoldingReadme.txt"",
        ""OutputPath"": ""./ScaffoldingReadme.txt"",
        ""IsTemplate"": false,
        ""ShowInListFiles"": false
      }
    ]
  }
}
";
    }

    internal class IdentityGeneratorFile
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string OutputPath { get; set; }
        public bool IsTemplate { get; set; }
        public bool ShowInListFiles { get; set; } = true;
    }

    internal class IdentityGeneratorFiles
    {
        public Dictionary<string, IdentityGeneratorFile[]> NamedFileConfig { get; set; }
    }
}