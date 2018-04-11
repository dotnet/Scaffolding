// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class RequiredFileEntity
    {
        public RequiredFileEntity(string outputPath, string templateName)
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
        }
        
        public RequiredFileEntity(string outputPath, string templateName, List<string> altPaths)
            : this(outputPath, templateName)
        {
            if (altPaths != null)
            {
                AltPaths = altPaths;
            }
        }

        /// <summary>
        /// Name of the template file.
        /// </summary>
        public string TemplateName { get; private set; }
        
        /// <summary>
        /// Path Relative to the .csproj file
        /// </summary>
        public string OutputPath { get; private set; }

        /// <summary>
        /// A list of other paths to check for file existence - to avoid generating new versions of existing files when not appropriate).
        /// </summary>
        public List<string> AltPaths { get; private set; } = new List<string>();
    }
}
