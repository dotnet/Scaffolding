// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    public class ViewGeneratorModel
    {
        private readonly IModelTypesLocator _modelTypesLocator;
        private ITypeSymbol _model;
        private ITypeSymbol _dataContext;

        public ViewGeneratorModel([NotNull]IModelTypesLocator modelTypesLocator)
        {
            _modelTypesLocator = modelTypesLocator;
        }

        [Option(Name = "model", ShortName = "m", Description = "Model class to use")]
        public string ModelClass { get; set; }

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "partialView", ShortName = "partial")]
        public bool PartialView { get; set; }

        [Option(Name = "referenceScriptLibraries", ShortName = "scripts")]
        public bool ReferenceScriptLibraries { get; set; }

        [Option(Name = "layout", ShortName = "l", Description = "Layout page to use, pass empty string if set in a Razor _viewStart file")]
        public string LayoutPage { get; set; }

        public void Validate()
        {
            string validationMessage;

            if (!TryValidateType(ModelClass, "model", out _model, out validationMessage) ||
                !TryValidateType(DataContextClass, "dataContext", out _dataContext, out validationMessage))
            {
                throw new Exception(validationMessage); 
            }
        }

        public ITypeSymbol ModelType
        {
            get
            {
                return _model;
            }
        }

        public ITypeSymbol DataContextType
        {
            get
            {
                return _dataContext;
            }
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

            var candidateModelTypes = _modelTypesLocator.GetType(typeName);
            if (!candidateModelTypes.Any())
            {
                errorMessage = string.Format("A type with the name {0} does not exist", typeName);
                return false;
            }

            if (candidateModelTypes.Count() > 1)
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
    }
}