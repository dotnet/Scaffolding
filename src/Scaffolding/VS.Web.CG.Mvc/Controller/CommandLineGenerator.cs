// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    [Alias("controller")]
    public class CommandLineGenerator : ICodeGenerator
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandLineGenerator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task GenerateCode(CommandLineGeneratorModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            ControllerGeneratorBase generator = null;

            if (string.IsNullOrEmpty(model.ModelClass))
            {
                generator = GetGenerator<ControllerEmpty>(); //This need to handle the WebAPI Empty as well...
            }
            else
            {
                generator = GetGenerator<ControllerWithContextGenerator>();
            }

            if (generator != null)
            {
                await generator.Generate(model);
            }
        }

        private ControllerGeneratorBase GetGenerator<TChild>() where TChild : ControllerGeneratorBase
        {
            return (ControllerGeneratorBase)ActivatorUtilities.CreateInstance<TChild>(_serviceProvider);
        }
    }
}
