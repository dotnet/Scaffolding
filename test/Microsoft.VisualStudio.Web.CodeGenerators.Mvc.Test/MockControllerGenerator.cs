using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Test
{
    public class MockControllerGenerator : ControllerGeneratorBase
    {
        public MockControllerGenerator(
            ILibraryManager libraryManager,
            IApplicationInfo applicationInfo,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(libraryManager, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {
        }

        public override Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            ValidateNameSpaceName(controllerGeneratorModel);
            var outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            return Task.CompletedTask;
        }

        protected override string GetTemplateName(CommandLineGeneratorModel controllerGeneratorModel)
        {
            throw new NotImplementedException();
        }
    }
}
