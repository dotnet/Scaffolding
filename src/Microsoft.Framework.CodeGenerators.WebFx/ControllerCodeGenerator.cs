// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    public class ControllerCodeGenerator
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

        public void GenerateCode([NotNull]string model,
            [NotNull]string dataContext,
            bool generateViews = false,
            bool referenceScriptLibraries = false)
        {
            _logger.LogMessage("Values passed:");
            _logger.LogMessage("Model: " + model);
            _logger.LogMessage("DataContext: " + dataContext);
            _logger.LogMessage("GenerateViews: " + generateViews);
            _logger.LogMessage("ReferenceScriptLibraries: " + referenceScriptLibraries);
        }
    }
}