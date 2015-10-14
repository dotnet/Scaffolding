using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    [Alias("controller")]
    public class ControllerGenerator : ICodeGenerator
    {
        private readonly IServiceProvider _serviceProvider;

        public ControllerGenerator([NotNull]IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task GenerateCode([NotNull]ControllerGeneratorModel model)
        {
            ControllerGeneratorBase generator = null;

            if (model.IsRestController)
            {
                if (string.IsNullOrEmpty(model.ModelClass))
                {
                    if (model.GenerateReadWriteActions)
                    {
                        //WebAPI with Actions
                    }
                    else
                    {
                        //Empty Web API
                    }
                }
                else
                {
                    //WebAPI With Context
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.ModelClass))
                {
                    if (model.GenerateReadWriteActions)
                    {
                        //Mvc with Actions
                    }
                    else
                    {
                        generator = GetGenerator<MvcControllerEmpty>();
                    }
                }
                else
                {
                    generator = GetGenerator<MvcControllerWithContext>();
                }
            }

            if (generator != null)
            {
                await generator.Generate(model);
            }
            else
            {
                // Just throwing as I enable this functionality, should remove it once I fill all the above...
                throw new Exception("Functionality not yet enabled...");
            }
        }

        private ControllerGeneratorBase GetGenerator<TChild>() where TChild : ControllerGeneratorBase
        {
            return (ControllerGeneratorBase)ActivatorUtilities.CreateInstance<TChild>(_serviceProvider);
        }
    }
}
