using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGeneration
{
    public class CodeGenCommand
    {
        public IServiceProvider ServiceProvider { get; set; }
        public CodeGenCommand(IServiceProvider serviceProvider)
        {
            if(serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            ServiceProvider = serviceProvider;
        }

        public int Execute(string [] args)
        {
            //var serviceProvider = new ServiceProvider();
            printargs(args);
            var generatorsLocator = ServiceProvider.GetRequiredService<ICodeGeneratorLocator>();
            var logger = ServiceProvider.GetRequiredService<ILogger>();

            if (args == null || args.Length == 0 || IsHelpArgument(args[0]))
            {
                ShowCodeGeneratorList(ServiceProvider, generatorsLocator.CodeGenerators);
                return 0;
            }
            Console.WriteLine("Execution started...");
            try
            {
                var codeGeneratorName = args[0];
                Console.WriteLine("Finding the generator '" + codeGeneratorName + "'...");
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
                logger.LogMessage(ex.StackTrace, LogMessageLevel.Error);
            }
            return 0;
        }

        private void printargs(string[] args)
        {
            if(args == null)
            {
                Console.WriteLine("Args were null");
                return;
            }
            Console.WriteLine("Args in Code Gen command :: ");
            foreach(var a in args)
            {
                Console.WriteLine("    " + a);
            }
        }

        private static bool IsHelpArgument(string argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            return string.Equals("-h", argument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("-?", argument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("--help", argument, StringComparison.OrdinalIgnoreCase);
        }

        private static void ShowCodeGeneratorList(IServiceProvider serviceProvider, IEnumerable<CodeGeneratorDescriptor> codeGenerators)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            Console.WriteLine("Showing Code Gen usage");
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
    }
}
