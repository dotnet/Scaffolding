// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.CodeGeneration
{
    internal static class TypeUtilities
    {
        public static void GetTypeNameandNamespace(string fullTypeName, out string typeName, out string namespaceName)
        {
            ExceptionUtilities.ValidateStringArgument(fullTypeName, "fullTypeName");

            var index = fullTypeName.LastIndexOf(".");
            if (index == -1)
            {
                typeName = fullTypeName;
                namespaceName = string.Empty;
            }
            else
            {
                typeName = fullTypeName.Substring(index + 1);
                namespaceName = fullTypeName.Substring(0, index);
            }
        }
    }
}