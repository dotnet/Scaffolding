using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Scaffold
{
    public class Program
    {
        private const string SCAFFOLD_COMMAND = "scaffold";
        private const string AREA_COMMAND = "area";
        private const string CONTROLLER_COMMAND = "controller";
        private const string IDENTITY_COMMAND = "identity";
        private const string RAZORPAGE_COMMAND = "razorpage";
        private const string VIEW_COMMAND = "view";
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

            var identityCommand = new Command(IDENTITY_COMMAND, "Scaffolds Identity");
            rootCommand.AddCommand(identityCommand);

            var viewCommand = new Command(VIEW_COMMAND, "Scaffolds a View");
            rootCommand.AddCommand(viewCommand);

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
                    return VisualStudio.Web.CodeGeneration.Tools.Program.Main(args);
                default:
                    return 0;
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
                description: "Target Framework to use.For example, net46.")
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
                description: "Scaffolds an Area.")
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
        private static Command ScaffoldControllerCommand() =>
            new Command(
                name: CONTROLLER_COMMAND,
                description: "Scaffolds a Controller")
            {
                // Arguments & Options
               ControllerNameOption(), AsyncActionsOption(), GenerateNoViewOption(), RestWithNoViewOption(), ReadWriteActionOption(),
               ModelClassOption(), DataContextOption(), BootStrapVersionOption(), ReferenceScriptLibrariesOption(), CustomLayoutOption(), UseDefaultLayoutOption(), OverwriteFilesOption(), RelativeFolderPathOption()
            };

        private static Option NamespaceNameOption() =>
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

        private static Command ScaffoldRazorPageCommand() =>
            new Command(
                name: RAZORPAGE_COMMAND,
                description: "Scaffolds Razor pages.")
            {
                // Arguments & Options
                NamespaceNameOption(), PartialViewOption(), NoPageModelOption(),
                ModelClassOption(), DataContextOption(), BootStrapVersionOption(), ReferenceScriptLibrariesOption(), CustomLayoutOption(), UseDefaultLayoutOption(), OverwriteFilesOption(), RelativeFolderPathOption()
            };
    }
}
