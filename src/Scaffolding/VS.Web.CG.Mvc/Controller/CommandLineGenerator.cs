// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.DotNet.Scaffolding.Shared;

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

            if (Enum.TryParse(typeof(DbType), model.DatabaseTypeString, ignoreCase: true, out var databaseType))
            {
                model.DatabaseType = (DbType)databaseType;
            }
            else
            {
                throw new ArgumentNullException("bad database type");
            }

            ControllerGeneratorBase generator = null;

            if (string.IsNullOrEmpty(model.ModelClass))
            {
                if (model.GenerateReadWriteActions)
                {
                    generator = GetGenerator<MvcControllerWithReadWriteActionGenerator>();
                }
                else
                {
                    generator = GetGenerator<MvcControllerEmpty>(); //This need to handle the WebAPI Empty as well...
                }
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
