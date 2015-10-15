using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.EntityFramework;
using Microsoft.Extensions.CodeGenerators.Mvc.Dependency;
using Microsoft.Extensions.CodeGenerators.Mvc.View;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class MvcControllerWithContext : ControllerWithContextGenerator
    {
        private readonly List<string> _views = new List<string>()
        {
            "Create",
            "Edit",
            "Details",
            "Delete",
            "List"
        };

        public MvcControllerWithContext(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IEntityFrameworkService entityFrameworkService,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILogger logger)
            : base(libraryManager, environment, modelTypesLocator, entityFrameworkService, codeGeneratorActionsService, serviceProvider, logger)
        {
        }

        public override async Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            Contract.Assert(!String.IsNullOrEmpty(controllerGeneratorModel.ModelClass));

            string outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);

            var model = ValidationUtil.ValidateType(controllerGeneratorModel.ModelClass, "model", ModelTypesLocator);
            var dataContext = ValidationUtil.ValidateType(controllerGeneratorModel.DataContextClass, "dataContext", ModelTypesLocator, throwWhenNotFound: false);

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");

            var dbContextFullName = dataContext != null ? dataContext.FullName : controllerGeneratorModel.DataContextClass;
            var modelTypeFullName = model.FullName;

            var modelMetadata = await EntityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(ServiceProvider);
            await layoutDependencyInstaller.Execute();

            if (string.IsNullOrEmpty(controllerGeneratorModel.ControllerName))
            {
                //Todo: Pluralize model name
                controllerGeneratorModel.ControllerName = model.Name + Constants.ControllerSuffix;
            }

            var templateName = "ControllerWithContext.cshtml";
            var templateModel = new ControllerWithContextTemplateModel(model, dbContextFullName)
            {
                ControllerName = controllerGeneratorModel.ControllerName,
                AreaName = string.Empty, //ToDo
                UseAsync = controllerGeneratorModel.UseAsync,
                ControllerNamespace = GetControllerNamespace(),
                ModelMetadata = modelMetadata
            };

            await CodeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            Logger.LogMessage("Added Controller : " + outputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));

            if (!controllerGeneratorModel.NoViews)
            {
                foreach (var viewTemplate in _views)
                {
                    var viewName = viewTemplate == "List" ? "Index" : viewTemplate;
                    // ToDo: This is duplicated from ViewGenerator.
                    bool isLayoutSelected = controllerGeneratorModel.UseDefaultLayout ||
                        !String.IsNullOrEmpty(controllerGeneratorModel.LayoutPage);

                    var viewTemplateModel = new ViewGeneratorTemplateModel()
                    {
                        ViewDataTypeName = modelTypeFullName,
                        ViewDataTypeShortName = model.Name,
                        ViewName = viewName,
                        LayoutPageFile = controllerGeneratorModel.LayoutPage,
                        IsLayoutPageSelected = isLayoutSelected,
                        IsPartialView = false,
                        ReferenceScriptLibraries = controllerGeneratorModel.ReferenceScriptLibraries,
                        ModelMetadata = modelMetadata,
                        JQueryVersion = "1.10.2"
                    };

                    // Todo: Need logic for areas
                    var viewOutputPath = Path.Combine(
                        ApplicationEnvironment.ApplicationBasePath,
                        Constants.ViewsFolderName,
                        templateModel.ControllerRootName,
                        viewName + Constants.ViewExtension);

                    await CodeGeneratorActionsService.AddFileFromTemplateAsync(viewOutputPath,
                        viewTemplate + Constants.RazorTemplateExtension, TemplateFolders, viewTemplateModel);

                    Logger.LogMessage("Added View : " + viewOutputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));
                }
            }

            await layoutDependencyInstaller.InstallDependencies();
        }
    }
}
