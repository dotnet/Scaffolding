// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    [Alias("view")]
    public class ViewGenerator : ICodeGenerator
    {
        private readonly ILogger _logger;
        private readonly IModelTypesLocator _modelTypesLocator;

        // Todo: Instead of each generator taking ILogger provide it in some base class?
        // However for it to be effective, it should be property dependecy injection rather
        // than constructor injection.
        public ViewGenerator(
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]ILogger logger)
        {
            _modelTypesLocator = modelTypesLocator;
            _logger = logger;
        }

        public void GenerateCode([NotNull]ViewGeneratorModel viewGeneratorModel)
        {
            // Validate model
            string validationMessage;
            ITypeSymbol model, dataContext;

            if (!TryValidateType(viewGeneratorModel.ModelClass, "model", out model, out validationMessage) ||
                !TryValidateType(viewGeneratorModel.DataContextClass, "dataContext", out dataContext, out validationMessage))
            {
                throw new Exception(validationMessage);
            }

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");
            Contract.Assert(dataContext != null, "Validation succeded but DataContext type not set");

            _logger.LogMessage("Provided Model Type: " + FullNameForType(model));
            _logger.LogMessage("Provided DataContext Type: " + FullNameForType(dataContext));
        }

        private bool TryValidateType(string typeName, string argumentName,
            out ITypeSymbol type, out string errorMessage)
        {
            errorMessage = string.Empty;
            type = null;

            if (string.IsNullOrEmpty(typeName))
            {
                //Perhaps for these kind of checks, the validation could be in the API.
                errorMessage = string.Format("Please provide a valid {0}", argumentName);
                return false;
            }

            var candidateModelTypes = _modelTypesLocator.GetType(typeName).ToList();

            int count = candidateModelTypes.Count;
            if (count == 0)
            {
                errorMessage = string.Format("A type with the name {0} does not exist", typeName);
                return false;
            }

            if (count > 1)
            {
                errorMessage = string.Format(
                    "Multiple types matching the name {0} exist:{1}, please use a fully qualified name",
                    typeName,
                    string.Join(",", candidateModelTypes.Select(t => t.Name).ToArray()));
                return false;
            }

            type = candidateModelTypes.First();
            return true;
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