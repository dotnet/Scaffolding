using System;
using System.Collections.Generic;
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
            Console.WriteLine("Attach Debugger");
            Console.Read();

            var generatorsLocator = _serviceProvider.GetService<ICodeGeneratorLocator>();

            if (args == null || args.Length == 0)
            {
                ShowCodeGeneratorList(generatorsLocator.CodeGenerators);
                return;
            }

            var codeGeneratorName = args[0];
            var generatorDescriptor = generatorsLocator.GetCodeGenerator(codeGeneratorName);

            var actionInvoker = new ActionInvoker(generatorDescriptor.CodeGeneratorAction);

            actionInvoker.Execute(args);
        }

        private void ShowCodeGeneratorList(IEnumerable<CodeGeneratorDescriptor> codeGenerators)
        {
            var logger = _serviceProvider.GetService<ILogger>();

            if (codeGenerators.Any())
            {
                logger.LogMessage("k gen <code generator name>");

                foreach (var generator in codeGenerators)
                {
                    logger.LogMessage(generator.Name);
                }
            }
            else
            {
                logger.LogMessage("There are no code generators installed to run");
            }
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

            serviceProvider.AddServiceWithDependencies<IModelTypesLocator, ModelTypesLocator>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorActionsService, CodeGeneratorActionsService>();
            serviceProvider.AddServiceWithDependencies<IDbContextEditorServices, DbContextEditorServices>();
            serviceProvider.AddServiceWithDependencies<IEntityFrameworkService, EntityFrameworkServices>();
        }
    }
}
