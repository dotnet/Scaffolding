// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class ClassNameModel
    {
        public ClassNameModel(string className, string namespaceName)
        {
            ExceptionUtilities.ValidateStringArgument(className, "className");

            ClassName = className;
            NamespaceName = namespaceName;
        }

        public ClassNameModel(string fullTypeName)
        {
            ExceptionUtilities.ValidateStringArgument(fullTypeName, "fullTypeName");

            var index = fullTypeName.LastIndexOf(".");
            if (index == -1)
            {
                ClassName = fullTypeName;
                NamespaceName = string.Empty;
            }
            else
            {
                ClassName = fullTypeName.Substring(index + 1);
                NamespaceName = fullTypeName.Substring(0, index);
            }
        }

        public string ClassName { get; private set; }

        public string NamespaceName { get; private set; }
    }
}