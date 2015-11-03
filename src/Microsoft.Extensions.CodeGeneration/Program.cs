using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CodeGeneration.EntityFramework;
using Microsoft.Extensions.CodeGeneration.Templating;
using Microsoft.Extensions.CodeGeneration.Templating.Compilation;
using Microsoft.Extensions.Compilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration
{
    public class Program
    {
        private static ServiceProvider _serviceProvider;

        public static void Main(string[] args)
        {
            var _serviceProvider = new ServiceProvider();

            AddCodeGenerationServices(_serviceProvider);
            //Debugger.Launch();

            var generatorsLocator = _serviceProvider.GetRequiredService<ICodeGeneratorLocator>();
            var logger = _serviceProvider.GetRequiredService<ILogger>();

            if (args == null || args.Length == 0 || IsHelpArgument(args[0]))
            {
                ShowCodeGeneratorList(generatorsLocator.CodeGenerators);
                return;
            }

            try
            {
                var codeGeneratorName = args[0];

                logger.LogMessage("Finding the generator '" + codeGeneratorName + "'...");
                var generatorDescriptor = generatorsLocator.GetCodeGenerator(codeGeneratorName);

                var actionInvoker = new ActionInvoker(generatorDescriptor.CodeGeneratorAction);

                logger.LogMessage("Running the generator '" + codeGeneratorName + "'...");
                actionInvoker.Execute(args);
            }
            catch (Exception ex)
            {
                while (ex is TargetInvocationException)
                {
                    ex = ex.InnerException;
                }
                logger.LogMessage(ex.Message, LogMessageLevel.Error);
            }
        }

        private static void ShowCodeGeneratorList(IEnumerable<CodeGeneratorDescriptor> codeGenerators)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger>();

            if (codeGenerators.Any())
            {
                logger.LogMessage("Usage:  dnx gen [code generator name]\n");
                logger.LogMessage("Code Generators:");

                foreach (var generator in codeGenerators)
                {
                    logger.LogMessage(generator.Name);
                }

                logger.LogMessage("\nTry dnx gen [code generator name] -? for help about specific code generator.");
            }
            else
            {
                logger.LogMessage("There are no code generators installed to run.");
            }
        }

        private static bool IsHelpArgument([NotNull]string argument)
        {
            return string.Equals("-h", argument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("-?", argument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("--help", argument, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddCodeGenerationServices([NotNull]ServiceProvider serviceProvider)
        {
            serviceProvider.Add(typeof(IApplicationEnvironment), PlatformServices.Default.Application);
            serviceProvider.Add(typeof(IRuntimeEnvironment), PlatformServices.Default.Runtime);
            serviceProvider.Add(typeof(IAssemblyLoadContextAccessor), PlatformServices.Default.AssemblyLoadContextAccessor);
            serviceProvider.Add(typeof(IAssemblyLoaderContainer), PlatformServices.Default.AssemblyLoaderContainer);
            serviceProvider.Add(typeof(ILibraryManager), PlatformServices.Default.LibraryManager);
            serviceProvider.Add(typeof(ILibraryExporter), CompilationServices.Default.LibraryExporter);
            serviceProvider.Add(typeof(ICompilerOptionsProvider), CompilationServices.Default.CompilerOptionsProvider);

            //Ordering of services is important here
            serviceProvider.Add(typeof(ILogger), new ConsoleLogger());
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<ICodeGeneratorAssemblyProvider, DefaultCodeGeneratorAssemblyProvider>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorLocator, CodeGeneratorsLocator>();

            serviceProvider.AddServiceWithDependencies<ICompilationService, RoslynCompilationService>();
            serviceProvider.AddServiceWithDependencies<ITemplating, RazorTemplating>();

            serviceProvider.AddServiceWithDependencies<IPackageInstaller, PackageInstaller>();

            serviceProvider.AddServiceWithDependencies<IModelTypesLocator, ModelTypesLocator>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorActionsService, CodeGeneratorActionsService>();
            serviceProvider.AddServiceWithDependencies<IDbContextEditorServices, DbContextEditorServices>();
            serviceProvider.AddServiceWithDependencies<IEntityFrameworkService, EntityFrameworkServices>();
        }
    }
}
