// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.CodeGeneration.Templating;

namespace Microsoft.Framework.CodeGeneration
{
    public class CodeGeneratorActionsService : ICodeGeneratorActionsService
    {
        private readonly IFilesLocator _filesLocator;
        private readonly ITemplating _templatingService;

        public CodeGeneratorActionsService(
            ITemplating templatingService,
            IFilesLocator filesLocator)
        {
            _templatingService = templatingService;
            _filesLocator = filesLocator;
        }

        public async Task AddFileAsync(string outputPath, string sourceFilePath)
        {
            ExceptionUtilities.ValidateStringArgument(outputPath, "outputPath");
            ExceptionUtilities.ValidateStringArgument(sourceFilePath, "sourceFilePath");

            if (!File.Exists(sourceFilePath))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    "The provided file '{0}' does not exist. This method expects a fully qualified path of an existing file.",
                    sourceFilePath));
            }

            using (var fileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            {
                await AddFileHelper(outputPath, fileStream);
            }
        }

        public async Task AddFileFromTemplateAsync(string outputPath, string templateName,
            [NotNull]IEnumerable<string> templateFolders,
            [NotNull]object templateModel)
        {
            ExceptionUtilities.ValidateStringArgument(outputPath, "outputPath");
            ExceptionUtilities.ValidateStringArgument(templateName, "templateName");

            var templatePath = _filesLocator.GetFilePath(templateName, templateFolders);
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new InvalidOperationException(string.Format(
                    "Template file {0} not found within search paths {1}",
                    templateName,
                    string.Join(";", templateFolders)));
            }

            Contract.Assert(File.Exists(templatePath));
            var templateContent = File.ReadAllText(templatePath);

            var templateResult = await _templatingService.RunTemplateAsync(templateContent, templateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new InvalidOperationException(string.Format(
                    "There was an error running the template {0}: {1}",
                    templatePath,
                    templateResult.ProcessingException.Message));
            }

            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(templateResult.GeneratedText)))
            {
                await AddFileHelper(outputPath, sourceStream);
            }
        }

        private async Task AddFileHelper(string outputPath, Stream sourceStream)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            if (File.Exists(outputPath))
            {
                FileAttributes attributes = File.GetAttributes(outputPath);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(outputPath, attributes & ~FileAttributes.ReadOnly);
                }
            }

            using (var writeStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(writeStream);
            }
        }
    }
}