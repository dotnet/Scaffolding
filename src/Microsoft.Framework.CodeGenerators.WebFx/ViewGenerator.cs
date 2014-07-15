// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    [Alias("view")]
    public class ViewGenerator : ICodeGenerator
    {
        private readonly ILogger _logger;

        // Todo: Instead of each generator taking ILogger provide it in some base class?
        // However for it to be effective, it should be property dependecy injection rather
        // than constructor injection.
        public ViewGenerator([NotNull]ILogger logger)
        {
            _logger = logger;
        }

        public void GenerateCode([NotNull]ViewGeneratorModel viewGeneratorModel)
        {
            viewGeneratorModel.Validate();

            Contract.Assert(viewGeneratorModel.ModelType != null, "Validation succeded but model type not set");
            Contract.Assert(viewGeneratorModel.DataContextType != null, "Validation succeded but DataContext type not set");

            _logger.LogMessage("Provided Model Type: " + FullNameForType(viewGeneratorModel.ModelType));
            _logger.LogMessage("Provided DataContext Type: " + FullNameForType(viewGeneratorModel.DataContextType));
        }

        // ToDo: Perhaps find some existing utility in Roslyn or provide this as API?
        private string FullNameForType([NotNull]ISymbol symbol)
        {
            if (symbol.ContainingNamespace != null & string.IsNullOrEmpty(symbol.ContainingNamespace.Name))
            {
                return symbol.Name;
            }
            return FullNameForType(symbol.ContainingNamespace) + "." + symbol.Name;
        }
    }
}