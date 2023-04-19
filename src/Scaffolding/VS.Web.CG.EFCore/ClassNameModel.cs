// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class ClassNameModel
    {
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

        public string ClassName { get; set; }

        public string NamespaceName { get; set; }
    }
}
