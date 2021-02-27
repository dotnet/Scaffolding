using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Scaffold
{
    public class Program
    {
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

        public async static Task Main(string[] args)
        {
            var rootCommand = GetScaffoldCommand();

            var areaCommand = GetScaffoldAreaCommand();
            rootCommand.AddCommand(areaCommand);

            var controllerCommand = new Command(CONTROLLER_COMMAND, "Scaffolds a Controller");
            rootCommand.AddCommand(controllerCommand);

            var identityCommand = new Command(IDENTITY_COMMAND, "Scaffolds Identity");
            rootCommand.AddCommand(identityCommand);

            var razorPageCommand = new Command(RAZORPAGE_COMMAND, "Scaffolds Razor Pages");
            rootCommand.AddCommand(razorPageCommand);

            var viewCommand = new Command(VIEW_COMMAND, "Scaffolds a View");
            rootCommand.AddCommand(viewCommand);

            rootCommand.Description = "dotnet scaffold [command] [-p|--project] [-n|--nuget-package-dir] [-c|--configuration] [-tfm|--target-framework] [-b|--build-base-path] [--no-build] ";
            if (args.Length == 0)
            {
                args = new string[] { "-h" };
            }

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseMiddleware(async (context, next) =>
            {
                string parsedCommandName = context.ParseResult.CommandResult.Command.Name;
                switch (parsedCommandName)
                {
                    case AREA_COMMAND:
                    case CONTROLLER_COMMAND:
                    case IDENTITY_COMMAND:
                    case RAZORPAGE_COMMAND:
                    case VIEW_COMMAND:
                        VisualStudio.Web.CodeGeneration.Tools.Program.Main(args);
                        return;
                    default:
                        await next(context);
                        break;
                }
            });

            commandLineBuilder.UseDefaults();
            var parser = commandLineBuilder.Build();
            await parser.InvokeAsync(args);
        }

        private static Command GetScaffoldCommand()
        {
            return new RootCommand()
            {
                new Option<string>(
                    new[] { "-p", "--project" },
                    "Specifies the path of the project file to run (folder name or full path). If not specified, it defaults to the current directory."
                )
                { IsRequired = false },
                new Option<string>(
                    new[] { "-n", "--nuget-package-dir" },
                    "Specifies the NuGet package directory."
                )
                { IsRequired = false },
                new Option<string>(
                    new[] {"-c", "--configuration" },
                     getDefaultValue: () => "Debug",
                    "Defines the build configuration. The default value is Debug."
                )
                { IsRequired = false },
                new Option<string>(
                    new[] {"-tfm", "--target-framework" },
                    "Target Framework to use. For example, net46."
                )
                { IsRequired = false },
                new Option<string>(
                    new[] {"-b", "--build-base-path" },
                    "The build base path."
                )
                { IsRequired = false },
                new Option<bool>(
                    "--no-build",
                    "Doesn't build the project before running. It also implicitly sets the --no-restore flag."
                )
                { IsRequired = false}
            };
        }

        private static Command GetScaffoldAreaCommand()
        {
            return new Command(AREA_COMMAND, "Scaffolds an Area")
            {
                new Argument<string>(
                    "AreaNameToGenerate",
                    "This tool is intended for ASP.NET Core web projects with controllers and views. It's not intended for Razor Pages apps."
                    )
                { Arity = ArgumentArity.ExactlyOne }
            };
        }
    }
}
