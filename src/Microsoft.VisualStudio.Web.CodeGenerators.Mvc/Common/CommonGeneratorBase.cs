// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    /// <summary>
    /// Common generator functionality for Controllers and Views
    /// </summary>
    public abstract class CommonGeneratorBase
    {
        protected CommonGeneratorBase(IApplicationInfo applicationInfo)
        {
            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            ApplicationInfo = applicationInfo;
        }

        protected IApplicationInfo ApplicationInfo
        {
            get;
            private set;
        }

        protected string ValidateAndGetOutputPath(CommonCommandLineModel commandLineModel, string outputFileName)
        {
            string outputFolder = String.IsNullOrEmpty(commandLineModel.RelativeFolderPath)
                ? ApplicationInfo.ApplicationBasePath
                : Path.Combine(ApplicationInfo.ApplicationBasePath, commandLineModel.RelativeFolderPath);

            var outputPath = Path.Combine(outputFolder, outputFileName);

            if (File.Exists(outputPath) && !commandLineModel.Force)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    MessageStrings.FileExists_useforce,
                    outputPath));
            }

            return outputPath;
        }
    }
}
