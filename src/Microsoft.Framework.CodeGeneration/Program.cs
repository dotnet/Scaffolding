using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Framework.CodeGeneration.EntityFramework;
using Microsoft.Framework.CodeGeneration.Templating;
using Microsoft.Framework.CodeGeneration.Templating.Compilation;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGeneration
{
    public class Program
    {
        private ServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = new ServiceProvider(fallbackServiceProvider: serviceProvider);
            AddCodeGenerationServices(_serviceProvider);
        }

        public void Main(string[] args)
        {
            //Debugger.Launch();
            //Debugger.Break();

            var generatorsLocator = _serviceProvider.GetService<ICodeGeneratorLocator>();

            if (args == null || args.Length == 0 || IsHelpArgument(args[0]))
            {
                ShowCodeGeneratorList(generatorsLocator.CodeGenerators);
                return;
            }

            var codeGeneratorName = args[0];
            var generatorDescriptor = generatorsLocator.GetCodeGenerator(codeGeneratorName);

            var actionInvoker = new ActionInvoker(generatorDescriptor.CodeGeneratorAction);

            try
            {
                actionInvoker.Execute(args);
            }
            catch (Exception ex)
            {
                var logger = _serviceProvider.GetService<ILogger>();
                logger.LogMessage(ex.Message);
            }
        }

        private void ShowCodeGeneratorList(IEnumerable<CodeGeneratorDescriptor> codeGenerators)
        {
            var logger = _serviceProvider.GetService<ILogger>();

            if (codeGenerators.Any())
            {
                logger.LogMessage("Usage:  k gen [code generator name]\n");
                logger.LogMessage("Code Generators:");

                foreach (var generator in codeGenerators)
                {
                    logger.LogMessage(generator.Name);
                }

                logger.LogMessage("\nTry k gen [code generator name] -? for help about specific code generator.");
            }
            else
            {
                logger.LogMessage("There are no code generators installed to run.");
            }
        }

        private bool IsHelpArgument([NotNull]string argument)
        {
            return string.Equals("-h", argument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("-?", argument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("--help", argument, StringComparison.OrdinalIgnoreCase);
        }

        private void AddCodeGenerationServices([NotNull]ServiceProvider serviceProvider)
        {
            //Ordering of services is important here
            ITypeActivator typeActivator = new TypeActivator();

            Contract.Assert(serviceProvider.GetServiceOrDefault<ITypeActivator>() == null);
            serviceProvider.Add(typeof(ITypeActivator), typeActivator);

            serviceProvider.Add(typeof(ILogger), new ConsoleLogger());
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<ICodeGeneratorAssemblyProvider, DefaultCodeGeneratorAssemblyProvider>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorLocator, CodeGeneratorsLocator>();

            serviceProvider.AddServiceWithDependencies<ICompilationService, RoslynCompilationService>();
            serviceProvider.AddServiceWithDependencies<ITemplating, RazorTemplating>();

            serviceProvider.AddServiceWithDependencies<IPackageInstaller, KpmPackageInstaller>();

            serviceProvider.AddServiceWithDependencies<IModelTypesLocator, ModelTypesLocator>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorActionsService, CodeGeneratorActionsService>();
            serviceProvider.AddServiceWithDependencies<IDbContextEditorServices, DbContextEditorServices>();
            serviceProvider.AddServiceWithDependencies<IEntityFrameworkService, EntityFrameworkServices>();
        }
    }
}
