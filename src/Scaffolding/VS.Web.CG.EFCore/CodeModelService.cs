// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class CodeModelService : ICodeModelService
    {
        private Workspace _workspace;
        private IProjectContext _projectContext;
        private ILogger _logger;
        private ICodeGenAssemblyLoadContext _loader;

        public CodeModelService(IProjectContext context, Workspace workspace, ILogger logger, ICodeGenAssemblyLoadContext loader)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _loader = loader;
            _projectContext = context;
            _workspace = workspace;

            _logger = logger;
        }

        public async Task<ContextProcessingResult> GetModelMetadata(ModelType modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var projectCompilation = await _workspace.CurrentSolution.Projects
                    .First(project => project.AssemblyName == _projectContext.AssemblyName)
                    .GetCompilationAsync();

            var reflectedTypesProvider = new ReflectedTypesProvider(
                projectCompilation,
                (c) => c,
                _projectContext,
                _loader,
                _logger);
            var modelReflectionType = reflectedTypesProvider.GetReflectedType(
                modelType: modelType.FullName,
                lookInDependencies: true);

            if (modelReflectionType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, modelType.Name));
            }

            var modelMetadata = new CodeModelMetadata(modelReflectionType);

            return new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextAvailable,
                ModelMetadata = modelMetadata
            };
        }
    }
}
