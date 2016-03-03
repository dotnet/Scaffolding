// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.CodeGeneration
{
    public class CodeGenCommand
    {
        private readonly ILogger _logger;
        private readonly ICodeGeneratorLocator _locator;

        public IServiceProvider ServiceProvider { get; set; }
        public CodeGenCommand(IServiceProvider serviceProvider, ILogger logger, ICodeGeneratorLocator locator)
        {
            if(serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            ServiceProvider = serviceProvider;
            _locator = locator;
            _logger = logger;
        }

        public int Execute(string [] args)
        {
            if (args == null || args.Length == 0 || IsHelpArgument(args[0]))
            {
                ShowCodeGeneratorList(_locator.CodeGenerators);
                return 0;
            }
            try
            {
                var codeGeneratorName = args[0];
                _logger.LogMessage("Finding the generator '" + codeGeneratorName + "'...");
                var generatorDescriptor = _locator.GetCodeGenerator(codeGeneratorName);

                var actionInvoker = new ActionInvoker(generatorDescriptor.CodeGeneratorAction);

                _logger.LogMessage("Running the generator '" + codeGeneratorName + "'...");
                actionInvoker.Execute(args);
            }
            catch (Exception ex)
            {
                while (ex is TargetInvocationException)
                {
                    ex = ex.InnerException;
                }
                _logger.LogMessage(ex.Message, LogMessageLevel.Error);
                _logger.LogMessage(ex.StackTrace, LogMessageLevel.Error);
            }
            return 0;
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

        private void ShowCodeGeneratorList(IEnumerable<CodeGeneratorDescriptor> codeGenerators)
        {
            if (codeGenerators.Any())
            {
                _logger.LogMessage("Usage:  dnx gen [code generator name]\n");
                _logger.LogMessage("Code Generators:");

                foreach (var generator in codeGenerators)
                {
                    _logger.LogMessage(generator.Name);
                }

                _logger.LogMessage("\nTry dnx gen [code generator name] -? for help about specific code generator.");
            }
            else
            {
                _logger.LogMessage("There are no code generators installed to run.");
            }
        }
    }
}
