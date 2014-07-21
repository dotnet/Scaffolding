// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    // For this CodeGenerator, it's optional to derive from ICodeGenerator because
    // name follows a supported convention.
    [Alias("controller")]
    public class ControllerCodeGenerator : ICodeGenerator
    {
        private IApplicationEnvironment _application;
        private ILogger _logger;

        //Todo: there will be a base class CodeGenerator that gives access to logger and
        //other most common services
        public ControllerCodeGenerator([NotNull]IApplicationEnvironment application,
            [NotNull]ILogger logger)
        {
            _application = application;
            _logger = logger;
        }

        // Multiple GenerateCode methods are not allowed.
        private void GenerateCode([NotNull]ControllerGeneratorModel model)
        {
            _logger.LogMessage("Values passed:");
            _logger.LogMessage("Model: " + model.ModelClass);
            _logger.LogMessage("DataContext: " + model.DataContextClass);
            _logger.LogMessage("GenerateViews: " + model.GenerateViews);
            _logger.LogMessage("ReferenceScriptLibraries: " + model.ReferenceScriptLibraries);
        }
    }
}