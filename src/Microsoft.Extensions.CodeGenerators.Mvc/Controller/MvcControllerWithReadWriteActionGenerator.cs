using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGenerators.Mvc.Dependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class MvcControllerWithReadWriteActionGenerator : MvcController
    {
        public MvcControllerWithReadWriteActionGenerator(ILibraryManager libraryManager,
            IApplicationEnvironment environment,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(libraryManager, environment, codeGeneratorActionsService, serviceProvider, logger)
        {
        }
        protected override string GetRequiredNameError
        {
            get
            {
                return CodeGenerators.Mvc.MessageStrings.ControllerNameRequired;
            }
        }
        protected override string GetTemplateName(CommandLineGeneratorModel generatorModel)
        {
            return generatorModel.IsRestController ? Constants.ApiControllerWithActionsTemplate : Constants.MvcControllerWithActionsTemplate;
        }
    }
}
