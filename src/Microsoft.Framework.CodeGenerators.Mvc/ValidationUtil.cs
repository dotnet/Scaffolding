// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    internal class ValidationUtil
    {
        public static bool TryValidateType(string typeName,
            string argumentName,
            IModelTypesLocator modelTypesLocator,
            out ITypeSymbol type,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            type = null;

            if (string.IsNullOrEmpty(typeName))
            {
                //Perhaps for these kind of checks, the validation could be in the API.
                errorMessage = string.Format("Please provide a valid {0}", argumentName);
                return false;
            }

            var candidateModelTypes = modelTypesLocator.GetType(typeName).ToList();

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
    }
}