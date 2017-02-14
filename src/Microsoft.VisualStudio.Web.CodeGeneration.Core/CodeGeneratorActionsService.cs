// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Core;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class CodeGeneratorActionsService : ICodeGeneratorActionsService
    {
        private readonly IFilesLocator _filesLocator;
        private readonly IFileSystem _fileSystem;
        private readonly ITemplating _templatingService;

        //public CodeGeneratorActionsService(
        //    ITemplating templatingService,
        //    IFilesLocator filesLocator)
        //    : this(templatingService, filesLocator, new DefaultFileSystem())
        //{
        //}

        public CodeGeneratorActionsService(
            ITemplating templatingService,
            IFilesLocator filesLocator,
            IFileSystem fileSystem)
        {
            _templatingService = templatingService;
            _filesLocator = filesLocator;
            _fileSystem = fileSystem;
        }

        public async Task AddFileAsync(string outputPath, string sourceFilePath)
        {
            ExceptionUtilities.ValidateStringArgument(outputPath, "outputPath");
            ExceptionUtilities.ValidateStringArgument(sourceFilePath, "sourceFilePath");

            if (!_fileSystem.FileExists(sourceFilePath))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    MessageStrings.FileNotFound,
                    sourceFilePath));
            }

            using (var fileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            {
                await AddFileHelper(outputPath, fileStream);
            }
        }

        public async Task AddFileFromTemplateAsync(string outputPath, string templateName,
            IEnumerable<string> templateFolders,
            object templateModel)
        {
            if (templateFolders == null)
            {
                throw new ArgumentNullException(nameof(templateFolders));
            }

            ExceptionUtilities.ValidateStringArgument(outputPath, "outputPath");
            ExceptionUtilities.ValidateStringArgument(templateName, "templateName");

            var templatePath = _filesLocator.GetFilePath(templateName, templateFolders);
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.TemplateFileNotFound,
                    templateName,
                    string.Join(";", templateFolders)));
            }

            Debug.Assert(_fileSystem.FileExists(templatePath));
            var templateContent = _fileSystem.ReadAllText(templatePath);

            var templateResult = await _templatingService.RunTemplateAsync(templateContent, templateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.TemplateProcessingError,
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
            _fileSystem.CreateDirectory(Path.GetDirectoryName(outputPath));

            if (_fileSystem.FileExists(outputPath))
            {
                _fileSystem.MakeFileWritable(outputPath);
            }

            await _fileSystem.AddFileAsync(outputPath, sourceStream);
        }
    }
}