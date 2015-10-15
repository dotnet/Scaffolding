using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.EntityFramework;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public abstract class ControllerWithContextGenerator : ControllerGeneratorBase
    {
        public ControllerWithContextGenerator(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IEntityFrameworkService entityFrameworkService,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILogger logger)
            : base(libraryManager, environment, codeGeneratorActionsService, serviceProvider, logger)
        {
            ModelTypesLocator = modelTypesLocator;
            EntityFrameworkService = entityFrameworkService;
        }

        // Todo: This method is duplicated with the ViewGenerator.
        protected async Task<ModelTypeAndContextModel> ValidateModelAndGetMetadata(CommonCommandLineModel commandLineModel)
        {
            ModelType model = ValidationUtil.ValidateType(commandLineModel.ModelClass, "model", ModelTypesLocator);
            ModelType dataContext = ValidationUtil.ValidateType(commandLineModel.DataContextClass, "dataContext", ModelTypesLocator, throwWhenNotFound: false);

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");

            var dbContextFullName = dataContext != null ? dataContext.FullName : commandLineModel.DataContextClass;

            var modelMetadata = await EntityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                DbContextFullName = dbContextFullName,
                ModelMetadata = modelMetadata
            };
        }

        protected IModelTypesLocator ModelTypesLocator
        {
            get;
            private set;
        }
        protected IEntityFrameworkService EntityFrameworkService
        {
            get;
            private set;
        }
    }
}
