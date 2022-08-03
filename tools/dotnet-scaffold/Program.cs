using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.DotNet.MSIdentity.Tool;

namespace Microsoft.DotNet.Tools.Scaffold
{
    public class Program
    {
        private const string SCAFFOLD_COMMAND = "scaffold";
        private const string AREA_COMMAND = "--area";
        private const string CONTROLLER_COMMAND = "--controller";
        private const string IDENTITY_COMMAND = "--identity";
        private const string RAZORPAGE_COMMAND = "--razorpage";
        private const string VIEW_COMMAND = "--view";
        /* 
        dotnet scaffold [generator] [-p|--project] [-n|--nuget-package-dir] [-c|--configuration] [-tfm|--target-framework] [-b|--build-base-path] [--no-build] 

        This commands supports the following generators :
            Area
            Controller
            Identity
            Razorpage
            View

        e.g: dotnet scaffold area <AreaNameToGenerate>
             dotnet scaffold identity
             dotnet scaffold razorpage 

        */

        public static int Main(string[] args)
        {
            var rootCommand = ScaffoldCommand();

            rootCommand.AddCommand(ScaffoldAreaCommand());
            rootCommand.AddCommand(ScaffoldControllerCommand());
            rootCommand.AddCommand(ScaffoldRazorPageCommand());
            rootCommand.AddCommand(ScaffoldViewCommand());
            rootCommand.AddCommand(ScaffoldIdentityCommand());
            //msidentity commands
            //internal commands
            var listAadAppsCommand = MSIdentity.Tool.Program.ListAADAppsCommand();
            var listServicePrincipalsCommand = MSIdentity.Tool.Program.ListServicePrincipalsCommand();
            var listTenantsCommand = MSIdentity.Tool.Program.ListTenantsCommand();
            var createClientSecretCommand = MSIdentity.Tool.Program.CreateClientSecretCommand();

            //exposed commands
            var registerApplicationCommand = MSIdentity.Tool.Program.RegisterApplicationCommand();
            var unregisterApplicationCommand = MSIdentity.Tool.Program.UnregisterApplicationCommand();
            var updateAppRegistrationCommand = MSIdentity.Tool.Program.UpdateAppRegistrationCommand();
            var updateProjectCommand = MSIdentity.Tool.Program.UpdateProjectCommand();
            var createAppRegistration = MSIdentity.Tool.Program.CreateAppRegistrationCommand();

            //hide internal commands.
            listAadAppsCommand.IsHidden = true;
            listServicePrincipalsCommand.IsHidden = true;
            listTenantsCommand.IsHidden = true;
            updateProjectCommand.IsHidden = true;
            createClientSecretCommand.IsHidden = true;

            listAadAppsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleListApps);
            listServicePrincipalsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleListServicePrincipals);
            listTenantsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleListTenants);
            registerApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleRegisterApplication);
            unregisterApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleUnregisterApplication);
            createAppRegistration.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleCreateAppRegistration);
            updateAppRegistrationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleUpdateApplication);
            updateProjectCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleUpdateProject);
            createClientSecretCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(MSIdentity.Tool.Program.HandleClientSecrets);
            rootCommand.AddCommand(listAadAppsCommand);
            rootCommand.AddCommand(listServicePrincipalsCommand);
            rootCommand.AddCommand(listTenantsCommand);
            rootCommand.AddCommand(registerApplicationCommand);
            rootCommand.AddCommand(unregisterApplicationCommand);
            rootCommand.AddCommand(createAppRegistration);
            rootCommand.AddCommand(updateAppRegistrationCommand);
            rootCommand.AddCommand(updateProjectCommand);
            rootCommand.AddCommand(createClientSecretCommand);

            rootCommand.Description = "dotnet scaffold [command] [-p|--project] [-n|--nuget-package-dir] [-c|--configuration] [-tfm|--target-framework] [-b|--build-base-path] [--no-build] ";
            if (args.Length == 0)
            {
                args = new string[] { "-h" };
            }

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseDefaults();
            var parser = commandLineBuilder.Build();
            int parseExitCode = parser.Invoke(args);
            if (parseExitCode != 0)
            {
                return parseExitCode;
            }

            ParseResult parseResult = parser.Parse(args);
            string parsedCommandName = parseResult.CommandResult.Command.Name;
            switch (parsedCommandName)
            {
                case AREA_COMMAND:
                case CONTROLLER_COMMAND:
                case IDENTITY_COMMAND:
                case RAZORPAGE_COMMAND:
                case VIEW_COMMAND:
                    if (parseResult.CommandResult.Children.Count == 1 &&
                        string.Equals(parseResult.CommandResult.Children[0].Symbol?.Name, "help", StringComparison.OrdinalIgnoreCase))
                    {
                        // The help option for the commands are handled by System.Commandline.
                        return 0;
                    }
                    args[0] = args[0].Replace("--", "");
                    return VisualStudio.Web.CodeGeneration.Tools.Program.Main(args);
                default:
                    // The command is not handled by 'dotnet scaffold'.
                    return -1;
            }
        }

        private static Option ProjectOption() =>
            new Option<string>(
                aliases: new[] { "-p", "--project" },
                description: "Specifies the path of the project file to run (folder name or full path). If not specified, it defaults to the current directory.")
            {
                IsRequired = false
            };

        private static Option NuGetPackageOption() =>
            new Option<string>(
                aliases: new[] { "-n", "--nuget-package-dir" },
                description: "Specifies the NuGet package directory.")
            {
                IsRequired = false
            };

        private static Option ConfigurationOption() =>
            new Option<string>(
                aliases: new[] { "-c", "--configuration" },
                getDefaultValue: () => "Debug",
                description: "Defines the build configuration. The default value is Debug.")
            {
                IsRequired = false
            };

        private static Option TargetFrameworkOption() =>
            new Option<string>(
                aliases: new[] { "-tfm", "--target-framework" },
                description: "Target Framework to use. For example, net46.")
            {
                IsRequired = false
            };

        private static Option BuildBasePathOption() =>
            new Option<string>(
                aliases: new[] { "-b", "--build-base-path" },
                description: "The build base path.")
            {
                IsRequired = false
            };

        private static Option NoBuildOption() =>
            new Option<bool>(
                aliases: new[] { "--no-build" },
                description: "Doesn't build the project before running. It also implicitly sets the --no-restore flag.")
            {
                IsRequired = false
            };

        private static Command ScaffoldCommand() =>
            new Command(
                name: SCAFFOLD_COMMAND,
                description: "Scaffold files to a .NET Application")
            {
                // Arguments & Options
                ProjectOption(), NuGetPackageOption(), ConfigurationOption(), TargetFrameworkOption(), BuildBasePathOption(), NoBuildOption()
            };

        // AREA SCAFFOLDERS
        private static Argument AreaNameArgument() =>
            new Argument<string>(
                name: "AreaNameToGenerate",
                description: "This tool is intended for ASP.NET Core web projects with controllers and views. It's not intended for Razor Pages apps.")
            {
                Arity = ArgumentArity.ExactlyOne
            };

        private static Command ScaffoldAreaCommand() =>
            new Command(
                name: AREA_COMMAND,
                description: "Scaffolds an Area")
            {
                // Arguments & Options
                AreaNameArgument()
            };

        private static Option ControllerNameOption() =>
            new Option<string>(
                aliases: new[] { "-name", "--controllerName" },
                description: "Name of the controller.")
            {
                IsRequired = true
            };

        private static Option AsyncActionsOption() =>
            new Option<bool>(
                aliases: new[] { "-async", "--useAsyncActions" },
                description: "Generate async controller actions.")
            {
                IsRequired = false
            };
        private static Option GenerateNoViewOption() =>
            new Option<bool>(
                aliases: new[] { "-nv", "--noViews" },
                description: "Generate no views.")
            {
                IsRequired = false
            };
        private static Option RestWithNoViewOption() =>
            new Option<bool>(
                aliases: new[] { "-api", "--restWithNoViews" },
                description: "Generate a Controller with REST style API. noViews is assumed and any view related options are ignored.")
            {
                IsRequired = false
            };

        private static Option ReadWriteActionOption() =>
            new Option<bool>(
                aliases: new[] { "-actions", "--readWriteActions" },
                description: "Generate controller with read/write actions without a model.")
            {
                IsRequired = false
            };

        private static Option ModelClassOption() =>
            new Option<string>(
                aliases: new[] { "-m", "--model" },
                description: "Model class to use.")
            {
                IsRequired = false
            };

        private static Option DataContextOption() =>
            new Option<string>(
                aliases: new[] { "-dc", "--dataContext" },
                description: "The DbContext class to use.")
            {
                IsRequired = false
            };

        private static Option BootStrapVersionOption() =>
            new Option<string>(
                aliases: new[] { "-b", "--bootstrapVersion" },
                description: "Specifies the bootstrap version. Valid values are 3 or 4. Default is 4. If needed and not present, a wwwroot directory is created that includes the bootstrap files of the specified version.")
            {
                IsRequired = false
            };

        private static Option ReferenceScriptLibrariesOption() =>
            new Option<bool>(
                aliases: new[] { "-scripts", "--referenceScriptLibraries" },
                description: "Reference script libraries in the generated views. Adds _ValidationScriptsPartial to Edit and Create pages.")
            {
                IsRequired = false
            };

        private static Option CustomLayoutOption() =>
            new Option<string>(
                aliases: new[] { "-l", "--layout" },
                description: "Custom Layout page to use.")
            {
                IsRequired = false
            };

        private static Option UseDefaultLayoutOption() =>
            new Option<bool>(
                aliases: new[] { "-udl", "--useDefaultLayout" },
                description: "Use the default layout for the views.")
            {
                IsRequired = false
            };

        private static Option OverwriteFilesOption() =>
            new Option<bool>(
                aliases: new[] { "-f", "--force" },
                description: "Overwrite existing files.")
            {
                IsRequired = false
            };

        private static Option RelativeFolderPathOption() =>
            new Option<string>(
                aliases: new[] { "-outDir", "--relativeFolderPath" },
                description: "The relative output folder path from project where the file are generated. If not specified, files are generated in the project folder.")
            {
                IsRequired = false
            };

        private static Option ControllerNamespaceOption() =>
            new Option<string>(
                aliases: new[] { "-namespace", "--controllerNamespace" },
                description: "Specify the name of the namespace to use for the generated controller.")
            {
                IsRequired = false
            };

        private static Option UseSQLliteOption() =>
            new Option<bool>(
                aliases: new[] { "-sqlite", "--useSqlite" },
                description: "Flag to specify if DbContext should use SQLite instead of SQL Server.")
            {
                IsRequired = false
            };

        private static Command ScaffoldControllerCommand() =>
            new Command(
                name: CONTROLLER_COMMAND,
                description: "Scaffolds a Controller")
            {
                // Arguments & Options
               ControllerNameOption(), AsyncActionsOption(), GenerateNoViewOption(), RestWithNoViewOption(), ReadWriteActionOption(),
               ModelClassOption(), DataContextOption(), BootStrapVersionOption(), ReferenceScriptLibrariesOption(), CustomLayoutOption(), UseDefaultLayoutOption(), OverwriteFilesOption(), RelativeFolderPathOption(),
               ControllerNamespaceOption(), UseSQLliteOption()
            };

        private static Option RazorpageNamespaceNameOption() =>
            new Option<string>(
                aliases: new[] { "-namespace", "--namespaceName" },
                description: "The name of the namespace to use for the generated PageModel.")
            {
                IsRequired = false
            };

        private static Option PartialViewOption() =>
            new Option<bool>(
                aliases: new[] { "-partial", "--partialView" },
                description: "Generate a partial view. Layout options -l and -udl are ignored if this is specified.")
            {
                IsRequired = false
            };

        private static Option NoPageModelOption() =>
            new Option<string>(
                aliases: new[] { "-npm", "--noPageModel" },
                description: "Switch to not generate a PageModel class for Empty template.")
            {
                IsRequired = false
            };

        private static Argument RazorPageNameArgument() =>
            new Argument<string>(
                name: "RazorpageNameToGenerate",
                description: "The name of the razor page to generate.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

        private static Argument TemplateNameArgument() =>
            new Argument<string>(
                name: "TemplateName",
                description: "Razor Pages or Views can be individually scaffolded by specifying the name of the new page/view and the template to use. The supported templates are: Empty|Create|Edit|Delete|Details|List")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

        private static Command ScaffoldRazorPageCommand() =>
            new Command(
                name: RAZORPAGE_COMMAND,
                description: "Scaffolds Razor pages")
            {
                // Arguments
                RazorPageNameArgument(), TemplateNameArgument(),

                // Options
                RazorpageNamespaceNameOption(), PartialViewOption(), NoPageModelOption(),
                ModelClassOption(), DataContextOption(), BootStrapVersionOption(), ReferenceScriptLibrariesOption(), CustomLayoutOption(), UseDefaultLayoutOption(), OverwriteFilesOption(), RelativeFolderPathOption(),
                UseSQLliteOption()
            };

        private static Argument ViewNameArgument() =>
            new Argument<string>(
                name: "ViewNameToGenerate",
                description: "The name of the View to generate.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

        private static Command ScaffoldViewCommand() =>
            new Command(
                name: VIEW_COMMAND,
                description: "Scaffolds a View")
            {
                // Arguments
                ViewNameArgument(),TemplateNameArgument(),

                // Options
                ModelClassOption(), DataContextOption(), BootStrapVersionOption(), ReferenceScriptLibrariesOption(), CustomLayoutOption(), UseDefaultLayoutOption(), OverwriteFilesOption(), RelativeFolderPathOption(),
                ControllerNamespaceOption(), UseSQLliteOption(), PartialViewOption()
            };

        private static Option DBContextOption() =>
            new Option<string>(
                aliases: new[] { "-dc", "--dbContext" },
                description: "Name of the DbContext to use, or generate (if it does not exist).")
            {
                IsRequired = false
            };

        private static Option FilesListOption() =>
            new Option<string>(
                aliases: new[] { "-fi", "--files" },
                description: "List of semicolon separated files to scaffold. Use the --listFiles option to see the available options.")
            {
                IsRequired = false
            };

        private static Option ListFilesOption() =>
            new Option<string>(
                aliases: new[] { "-lf", "--listFiles" },
                description: "Lists the files that can be scaffolded by using the '--files' option.")
            {
                IsRequired = false
            };

        private static Option UserClassOption() =>
            new Option<string>(
                aliases: new[] { "-u", "--userClass" },
                description: "Name of the User class to generate.")
            {
                IsRequired = false
            };

        private static Option UseDefaultUIOption() =>
            new Option<string>(
                aliases: new[] { "-udui", "--useDefaultUI" },
                description: "Use this option to setup identity and to use Default UI.")
            {
                IsRequired = false
            };

        private static Option GenerateLayoutOption() =>
            new Option<string>(
                aliases: new[] { "-gl", "--generateLayout" },
                description: "Use this option to generate a new _Layout.cshtml")
            {
                IsRequired = false
            };

        private static Command ScaffoldIdentityCommand() =>
            new Command(
                name: IDENTITY_COMMAND,
                description: "Scaffolds Identity")
            {
                // Options
                DBContextOption(), FilesListOption(), ListFilesOption(), UserClassOption(), UseSQLliteOption(), OverwriteFilesOption(), UseDefaultUIOption(), CustomLayoutOption(), 
                GenerateLayoutOption(), BootStrapVersionOption()
            };
    }
}
