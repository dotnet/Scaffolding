using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    [Alias("identity")]
    public class IdentityGenerator : ICodeGenerator
    {
        private const string IdentityAreaName = "Identity";
        private static readonly string[] AreaFolders = new string[]
        {
            "Controllers",
            "Data",
            "Extensions",
            "Pages",
            "Services"
        };

        private static Dictionary<string, string> Templates = new Dictionary<string,string>()
        {
            {"AccountController.cshtml", Path.Combine("Areas", "Identity","Controllers", "AccountController.cs")},
            {"ApplicationDbContext.cshtml", Path.Combine("Areas", "Identity","Data", "ApplicationDbContext.cs")},
            {"ApplicationUser.cshtml", Path.Combine("Areas", "Identity","Data", "ApplicationUser.cs")},
            {"EmailSenderExtensions.cshtml", Path.Combine("Areas", "Identity","Extensions", "EmailSenderExtensions.cs")},
            {"UrlHelperExtensions.cshtml", Path.Combine("Areas", "Identity","Extensions", "UrlHelperExtensions.cs")},
            {"EmailSender.cshtml", Path.Combine("Areas", "Identity","Services", "EmailSender.cs")},
            {"IEmailSender.cshtml", Path.Combine("Areas", "Identity","Services", "IEmailSender.cs")},
            {"_Layout.cshtml", Path.Combine("Areas", "Identity", "Pages", "_Layout.cshtml")},
            {"_LoginPartial.cshtml", Path.Combine("Areas", "Identity", "Pages", "_LoginPartial.cshtml")},
            {"_ViewImports.cshtml", Path.Combine("Areas", "Identity", "Pages", "_ViewImports.cshtml")},

            // Accounts
            {"Account.AccessDenied.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "AccessDenied.cshtml.cs")},
            {"Account.ConfirmEmail.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ConfirmEmail.cshtml.cs")},
            {"Account.ExternalLogin.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs")},
            {"Account.ForgotPasswordConfirmation.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPasswordConfirmation.cshtml.cs")},
            {"Account.LockOut.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "LockOut.cshtml.cs")},
            {"Account.Login.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Login.cshtml.cs")},
            {"Account.LoginWith2fa.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWith2fa.cshtml.cs")},
            {"Account.LoginWithRecoveryCode.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWithRecoveryCode.cshtml.cs")},
            {"Account.Register.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs")},
            {"Account.ResetPassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml.cs")},
            {"Account.ResetPasswordConfirmation.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPasswordConfirmation.cshtml.cs")},
            {"Account.SignedOut.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "SignedOut.cshtml.cs")},
            
            // Accounts/Manage
            {"Account.Manage._ViewImports.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_ViewImports.cshtml")},
            {"Account.Manage.ChangePassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml.cs")},
            {"Account.Manage.Disable2fa.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Disable2fa.cshtml.cs")},
            {"Account.Manage.EnableAuthenticator.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "EnableAuthenticator.cshtml.cs")},
            {"Account.Manage.ExternalLogins.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml.cs")},
            {"Account.Manage.GenerateRecoveryCodes.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "GenerateRecoveryCodes.cshtml.cs")},
            {"Account.Manage.Index.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml.cs")},
            {"Account.Manage.ResetAuthenticator.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ResetAuthenticator.cshtml.cs")},
            {"Account.Manage.SetPassword.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml.cs")},
            {"Account.Manage.TwoFactorAuthentication.cs.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "TwoFactorAuthentication.cshtml.cs")},
            {"Account.Manage.ManageNavPages.cshtml", Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ManageNavPages.cs")},
            
        };

        private static Dictionary<string, string> StaticFiles = new Dictionary<string, string>()
        {
            {Path.Combine("Pages", "_ValidationScriptsPartial.cshtml"), Path.Combine("Areas", "Identity", "Pages", "_ValidationScriptsPartial.cshtml")},
            {Path.Combine("Pages", "_ViewStart.cshtml"), Path.Combine("Areas", "Identity", "Pages", "_ViewStart.cshtml")},

            // Accounts
            {Path.Combine("Pages", "Account", "Account.AccessDenied.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "AccessDenied.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ConfirmEmail.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ConfirmEmail.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ExternalLogin.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ForgotPasswordConfirmation.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ForgotPasswordConfirmation.cshtml")},
            {Path.Combine("Pages", "Account", "Account.LockOut.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "LockOut.cshtml")},
            {Path.Combine("Pages", "Account", "Account.Login.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Login.cshtml")},
            {Path.Combine("Pages", "Account", "Account.LoginWith2fa.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWith2fa.cshtml")},
            {Path.Combine("Pages", "Account", "Account.LoginWithRecoveryCode.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "LoginWithRecoveryCode.cshtml")},
            {Path.Combine("Pages", "Account", "Account.Register.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Register.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ResetPassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml")},
            {Path.Combine("Pages", "Account", "Account.ResetPasswordConfirmation.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "ResetPasswordConfirmation.cshtml")},
            {Path.Combine("Pages", "Account", "Account.SignedOut.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "SignedOut.cshtml")},

            // Accounts/Manage
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage._Layout.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_Layout.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage._ManageNav.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_ManageNav.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage._StatusMessage.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "_StatusMessage.cshtml")},
            

            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.ChangePassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.Disable2fa.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Disable2fa.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.EnableAuthenticator.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "EnableAuthenticator.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.ExternalLogins.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.GenerateRecoveryCodes.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "GenerateRecoveryCodes.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.Index.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.ResetAuthenticator.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "ResetAuthenticator.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.SetPassword.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml")},
            {Path.Combine("Pages", "Account", "Manage", "Account.Manage.TwoFactorAuthentication.cshtml"), Path.Combine("Areas", "Identity", "Pages", "Account", "Manage", "TwoFactorAuthentication.cshtml")},

            {Path.Combine("wwwroot","css","site.css"), Path.Combine("wwwroot","identity","css","site.css")},
            {Path.Combine("wwwroot","css","site.min.css"), Path.Combine("wwwroot","identity","css","site.min.css")},
            {Path.Combine("wwwroot","js","site.js"), Path.Combine("wwwroot","identity","js","site.js")},
            {Path.Combine("wwwroot","js","site.min.js"), Path.Combine("wwwroot","identity","js","site.min.js")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.css"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.css")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap-theme.min.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap-theme.min.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.css"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.css")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","css","bootstrap.min.css.map"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","css","bootstrap.min.css.map")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.eot"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.eot")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.svg"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.svg")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.ttf"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.ttf")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff2"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","fonts","glyphicons-halflings-regular.woff2")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","js","bootstrap.js"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","bootstrap.js")},
            {Path.Combine("wwwroot","lib","bootstrap","dist","js","npm.js"), Path.Combine("wwwroot","identity","lib","bootstrap","dist","js","npm.js")},
            {Path.Combine("wwwroot","lib","jquery","LICENSE.txt"), Path.Combine("wwwroot","identity","lib","jquery","LICENSE.txt")},
            {Path.Combine("wwwroot","lib","jquery-validation","LICENSE.md"), Path.Combine("wwwroot","identity","lib","jquery-validation","LICENSE.md")},
            {Path.Combine("wwwroot","lib","jquery-validation","dist","additional-methods.js"), Path.Combine("wwwroot","identity","lib","jquery-validation","dist","additional-methods.js")},
            {Path.Combine("wwwroot","lib","jquery-validation","dist","jquery.validate.js"), Path.Combine("wwwroot","identity","lib","jquery-validation","dist","jquery.validate.js")},
            {Path.Combine("wwwroot","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.js"), Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.js")},
            {Path.Combine("wwwroot","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.min.js"), Path.Combine("wwwroot","identity","lib","jquery-validation-unobtrusive","jquery.validate.unobtrusive.min.js")},
        };

        private ILogger _logger;
        private IApplicationInfo _applicationInfo;
        private IServiceProvider _serviceProvider;
        private ICodeGeneratorActionsService _codegeratorActionService;
        private IProjectContext _projectContext;

        public IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    Constants.ThisAssemblyName,
                    _applicationInfo.ApplicationBasePath,
                    new[]
                     { 
                         Path.Combine("Identity", "Controllers"),
                         Path.Combine("Identity", "Data"),
                         Path.Combine("Identity", "Extensions"),
                         Path.Combine("Identity", "Services"),
                         Path.Combine("Identity", "Pages"),
                     },
                    _projectContext);
            }
        }

        private string _templateFolderRoot;
        private string TemplateFolderRoot
        {
            get
            {
                if (string.IsNullOrEmpty(_templateFolderRoot))
                {
                    _templateFolderRoot = TemplateFoldersUtilities.GetTemplateFolders(
                        Constants.ThisAssemblyName,
                        _applicationInfo.ApplicationBasePath,
                        new [] { "Identity" },
                        _projectContext
                    ).First();
                }
                
                return _templateFolderRoot;
            }
        }

        public IdentityGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            ICodeGeneratorActionsService actionService,
            IProjectContext projectContext,
            ILogger logger)
        {
            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (actionService == null)
            {
                throw new ArgumentNullException(nameof(actionService));
            }

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _applicationInfo = applicationInfo;
            _serviceProvider = serviceProvider;
            _codegeratorActionService = actionService;
            _projectContext = projectContext;
            _logger = logger;
        }

        public async Task GenerateCode(IdentityGeneratorCommandLineModel commandlineModel)
        {
            // TODO: Generation logic.
            if (commandlineModel == null)
            {
                throw new ArgumentNullException(nameof(commandlineModel));
            }

            EnsureFolderLayout(IdentityAreaName);

            await AddTemplateFiles(commandlineModel);
            await AddStaticFiles();
        }

        private async Task AddStaticFiles()
        {
            foreach (var staticFile  in StaticFiles)
            {
                await _codegeratorActionService.AddFileAsync(
                    staticFile.Value,
                    Path.Combine(TemplateFolderRoot, staticFile.Key)
                );
            }
        }

        private async Task AddTemplateFiles(IdentityGeneratorCommandLineModel commandLineModel)
        {
            var model = new IdentityGeneratorTemplateModel()
            {
                Namespace = _projectContext.RootNamespace,
                DbContextNamespace = _projectContext.RootNamespace+".Areas.Identity.Data",
                ApplicationName = _applicationInfo.ApplicationName
            };

            foreach (var template in Templates)
            {
                await _codegeratorActionService.AddFileFromTemplateAsync(
                    template.Value,
                    template.Key,
                    TemplateFolders,
                    model
                );
            }
        }

        /// <summary>
        /// Creates a folder hierarchy:
        ///     ProjectDir
        ///        \ Areas
        ///            \ IdentityAreaName
        ///                \ Controllers
        ///                \ Data
        ///                \ Extensions
        ///                \ Pages
        ///                \ Services
        /// </summary>
        private void EnsureFolderLayout(string IdentityareaName)
        {
            var areaBasePath = Path.Combine(_applicationInfo.ApplicationBasePath, "Areas");
            if (!Directory.Exists(areaBasePath))
            {
                Directory.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, IdentityareaName);
            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            foreach (var areaFolder in AreaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
    }
}
