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
        private readonly IFileSystem _fileSystem;
        private readonly ITemplating _templatingService;

        public CodeGeneratorActionsService(
            ITemplating templatingService,
            IFilesLocator filesLocator)
            :this(templatingService, filesLocator, new DefaultFileSystem())
        {
        }

        internal CodeGeneratorActionsService(
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

            Contract.Assert(_fileSystem.FileExists(templatePath));
            var templateContent = _fileSystem.ReadAllText(templatePath);

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
            _fileSystem.CreateDirectory(Path.GetDirectoryName(outputPath));

            if (_fileSystem.FileExists(outputPath))
            {
                _fileSystem.MakeFileWritable(outputPath);
            }

            await _fileSystem.AddFileAsync(outputPath, sourceStream);
        }
    }
}