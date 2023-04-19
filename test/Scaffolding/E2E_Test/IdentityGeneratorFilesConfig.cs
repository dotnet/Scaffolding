// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public static class IdentityGeneratorFilesConfig
    {
        public enum LayoutFileDisposition
        {
            Generate,
            UseExisting,
            NoLayout
        }

        public static List<string> Templates = new List<string>()
        {
            Path.Combine("Pages", "Shared", "_Layout.cshtml"),
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

        // TODO: add more tests where some of the currently "invariant" content has dynamic locations
        // Specifically, deal with the "support file location" varying based on initial content in the project being scaffolded.
        // This currently assumes the support location is "Pages/Shared/", but it could possibly be "Views/Shared/" too.
        public static List<string> StaticFiles(LayoutFileDisposition layoutFileDisposition)
        {
            List<string> staticFiles = new List<string>(LocationInvariantStaticFiles);

            if (layoutFileDisposition != LayoutFileDisposition.Generate)
            {
                staticFiles.Add(Path.Combine("Areas", "Identity", "Pages", "_ValidationScriptsPartial.cshtml"));
            }

            if (layoutFileDisposition != LayoutFileDisposition.NoLayout)
            {
                staticFiles.Add(Path.Combine("Pages", "Shared", "_ValidationScriptsPartial.cshtml"));
            }

            return staticFiles;
        }

        private static IReadOnlyList<string> LocationInvariantStaticFiles = new List<string>()
        {
            //Path.Combine("Areas", "Identity", "Pages", "_ValidationScriptsPartial.cshtml"),
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

            "ScaffoldingReadme.txt"
        };
    }
}