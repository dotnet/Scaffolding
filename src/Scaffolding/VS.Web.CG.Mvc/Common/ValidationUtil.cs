// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class ValidationUtil
    {
        public static ModelType ValidateType(string typeName,
            string argumentName,
            IModelTypesLocator modelTypesLocator,
            bool throwWhenNotFound = true)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(string.Format(MessageStrings.ProvideValidArgument, argumentName));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            var candidateModelTypes = modelTypesLocator.GetType(typeName).ToList();

            int count = candidateModelTypes.Count;
            if (count == 0)
            {
                if (throwWhenNotFound)
                {
                    throw new ArgumentException(string.Format(MessageStrings.TypeDoesNotExist, typeName));
                }
                return null;
            }

            if (count > 1)
            {
                throw new ArgumentException(string.Format(
                    MessageStrings.MultipleTypesMatchingName,
                    typeName,
                    string.Join(",", candidateModelTypes.Select(t => t.Name).ToArray())));
            }

            return candidateModelTypes.First();
        }
    }
}
