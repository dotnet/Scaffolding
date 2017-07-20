// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class RequiredFileEntity
    {
        public RequiredFileEntity(string outputPath, string templateName, bool isStaticFile = true, dynamic templateModel = null)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            TemplateName = templateName;
            OutputPath = outputPath;
            IsStaticFile = isStaticFile;
            TemplateModel = templateModel;
        }
        
        /// <summary>
        /// Name of the template file.
        /// </summary>
        public string TemplateName { get; }
        
        /// <summary>
        /// Path Relative to the .csproj file
        /// </summary>
        public string OutputPath { get; }

        public bool IsStaticFile { get; }

        public dynamic TemplateModel { get; }
    }
}
