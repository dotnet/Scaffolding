// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
// ConsoleLogger
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.Core;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;


namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class CodeGeneratorActionsService : ICodeGeneratorActionsService
    {
        private readonly IFilesLocator _filesLocator;
        private readonly IFileSystem _fileSystem;
        private readonly ITemplating _templatingService;
        private readonly static ConsoleLogger _logger = new ConsoleLogger();

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
            _logger.LogMessage($"Rendering template {templatePath}\n", LogMessageLevel.Trace);
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

        public async Task<string> ExecuteTemplate(string templateName,
            IEnumerable<string> templateFolders,
            object templateModel)
        {
            if (templateFolders == null)
            {
                throw new ArgumentNullException(nameof(templateFolders));
            }

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
            _logger.LogMessage($"Rendering template {templatePath}\n", LogMessageLevel.Trace);
            var templateContent = _fileSystem.ReadAllText(templatePath);

            var templateResult = await _templatingService.RunTemplateAsync(templateContent, templateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.TemplateProcessingError,
                    templatePath,
                    templateResult.ProcessingException.Message));
            }

            return templateResult.GeneratedText;
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
