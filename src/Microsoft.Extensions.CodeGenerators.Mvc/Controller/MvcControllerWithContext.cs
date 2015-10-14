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
            ModelType model, dataContext;

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(ServiceProvider);
            model = ValidationUtil.ValidateType(controllerGeneratorModel.ModelClass, "model", ModelTypesLocator);
            dataContext = ValidationUtil.ValidateType(controllerGeneratorModel.DataContextClass, "dataContext", ModelTypesLocator, throwWhenNotFound: false);
            await GenerateController(controllerGeneratorModel, model, dataContext, GetControllerNamespace(), layoutDependencyInstaller);

            await layoutDependencyInstaller.InstallDependencies();
        }

        private async Task GenerateController(CommandLineGeneratorModel controllerGeneratorModel,
            ModelType model,
            ModelType dataContext,
            string controllerNameSpace,
            MvcLayoutDependencyInstaller layoutDependencyInstaller)
        {
            if (string.IsNullOrEmpty(controllerGeneratorModel.ControllerName))
            {
                //Todo: Pluralize model name
                controllerGeneratorModel.ControllerName = model.Name + Constants.ControllerSuffix;
            }

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");

            string outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);

            var dbContextFullName = dataContext != null ? dataContext.FullName : controllerGeneratorModel.DataContextClass;
            var modelTypeFullName = model.FullName;

            var modelMetadata = await EntityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

            await layoutDependencyInstaller.Execute();

            var templateName = "ControllerWithContext.cshtml";
            var templateModel = new ControllerWithContextTemplateModel(model, dbContextFullName)
            {
                ControllerName = controllerGeneratorModel.ControllerName,
                AreaName = string.Empty, //ToDo
                UseAsync = controllerGeneratorModel.UseAsync,
                ControllerNamespace = controllerNameSpace,
                ModelMetadata = modelMetadata
            };

            var appBasePath = ApplicationEnvironment.ApplicationBasePath;
            await CodeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            Logger.LogMessage("Added Controller : " + outputPath.Substring(appBasePath.Length));

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
                        appBasePath,
                        Constants.ViewsFolderName,
                        templateModel.ControllerRootName,
                        viewName + Constants.ViewExtension);

                    await CodeGeneratorActionsService.AddFileFromTemplateAsync(viewOutputPath,
                        viewTemplate + Constants.RazorTemplateExtension, TemplateFolders, viewTemplateModel);

                    Logger.LogMessage("Added View : " + viewOutputPath.Substring(appBasePath.Length));
                }
            }
        }
    }
}
