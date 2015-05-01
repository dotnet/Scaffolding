// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    internal static class TypeUtilities
    {
        public static ClassNameModel GetTypeNameandNamespace(string fullTypeName)
        {
            ExceptionUtilities.ValidateStringArgument(fullTypeName, "fullTypeName");

            var index = fullTypeName.LastIndexOf(".");
            if (index == -1)
            {
                return new ClassNameModel()
                {
                    ClassName = fullTypeName,
                    NamespaceName = string.Empty
                };
            }
            else
            {
                return new ClassNameModel()
                {
                    ClassName = fullTypeName.Substring(index + 1),
                    NamespaceName = fullTypeName.Substring(0, index)
                };
            }
        }
    }
}