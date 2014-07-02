using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Common.CommandLine;
using RuntimeOptionType = Microsoft.Framework.Runtime.Common.CommandLine.CommandOptionType;

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

            var factoryLocator = _serviceProvider.GetService<CodeGeneratorFactoriesLocator>();

            if (args == null || args.Length == 0)
            {
                ShowCodeGeneratorList(factoryLocator.CodeGeneratorFactories);
                return;
            }

            var codeGeneratorName = args[0];

            var factory = factoryLocator.GetCodeGeneratorFactory(codeGeneratorName);

            CommandLineApplication app = new CommandLineApplication();

            app.Command(codeGeneratorName, c =>
            {
                foreach (var argument in factory.CodeGeneratorMetadata.Arguments)
                {
                    c.Argument(argument.Name, argument.Description);
                }

                foreach (var option in factory.CodeGeneratorMetadata.Options)
                {
                    c.Option(template: option.ToTemplate(),
                        description: option.Description,
                        optionType: RuntimeOptionTypeFromOptionType(option.OptionType));
                }

                c.HelpOption("-h|-?|--help");

                c.Invoke = () =>
                {
                    //Todo: pass remaining args...
                    var invoker = new CodeGeneratorInvoker(factory,
                        _serviceProvider.GetService<ITypeActivator>(),
                        _serviceProvider);

                    var values = new Dictionary<string, object>();

                    foreach (var argument in c.Arguments)
                    {
                        values.Add(argument.Name, argument.Value);
                    }
                    foreach (var option in c.Options.Where(opt => opt.HasValue()))
                    {
                        if (option.OptionType == RuntimeOptionType.NoValue)
                        {
                            values.Add(option.LongName, true);
                        }
                        else
                        {
                            values.Add(option.LongName, option.Value());
                        }
                    }

                    invoker.Invoke(values);
                    return 0;
                };
            });

            app.Execute(args);
        }

        private RuntimeOptionType RuntimeOptionTypeFromOptionType(CommandOptionType optionType)
        {
            if (optionType == CommandOptionType.SingleValue)
            {
                return RuntimeOptionType.SingleValue;
            }

            if (optionType == CommandOptionType.Switch)
            {
                return RuntimeOptionType.NoValue;
            }

            Contract.Assert(false, "New CommandOptionType introduced for which there is no runtime option type");
            throw new Exception("Undetected command option type");
        }

        private void ShowCodeGeneratorList(IEnumerable<CodeGeneratorFactory> codeGeneratorFactories)
        {
            var logger = _serviceProvider.GetService<ILogger>();

            if (codeGeneratorFactories.Any())
            {
                logger.LogMessage("k gen <code generator name>");

                foreach (var factory in codeGeneratorFactories)
                {
                    logger.LogMessage(factory.CodeGeneratorMetadata.Name);
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
            serviceProvider.AddServiceWithDependencies<CodeGeneratorFactoriesLocator, CodeGeneratorFactoriesLocator>();
        }
    }
}
