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
        public static ModelType ValidateType(string typeName,
            string argumentName,
            IModelTypesLocator modelTypesLocator,
            bool throwWhenNotFound = true)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(string.Format("Please provide a valid {0}", argumentName));
            }

            var candidateModelTypes = modelTypesLocator.GetType(typeName).ToList();

            int count = candidateModelTypes.Count;
            if (count == 0)
            {
                if (throwWhenNotFound)
                {
                    throw new ArgumentException(string.Format("A type with the name {0} does not exist", typeName));
                }
                return null;
            }

            if (count > 1)
            {
                throw new ArgumentException(string.Format(
                    "Multiple types matching the name {0} exist:{1}, please use a fully qualified name",
                    typeName,
                    string.Join(",", candidateModelTypes.Select(t => t.Name).ToArray())));
            }

            return candidateModelTypes.First();
        }
    }
}