// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    //Todo: Make this internal and expose as a sevice
    public class RazorTemplating : ITemplating
    {
        private ICompilationService _compilationService;

        public RazorTemplating(ICompilationService compilationService)
        {
            if (compilationService == null)
            {
                throw new ArgumentNullException(nameof(compilationService));
            }

            _compilationService = compilationService;
        }

        public async Task<TemplateResult> RunTemplateAsync(string content,
            dynamic templateModel)
        {
            // Don't care about the RazorProject as we already have the content of the .cshtml file 
            // and don't need to deal with imports.
            var fileSystem = RazorProjectFileSystem.Create(Directory.GetCurrentDirectory());
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, (builder) =>
            {
                 RazorExtensions.Register(builder);

                builder.AddDefaultImports(DefaultImportItem.Instance);
            });

            var templateItem = new TemplateRazorProjectItem(content);
            var codeDocument = projectEngine.Process(templateItem);
            var generatorResults = codeDocument.GetCSharpDocument();

            if (generatorResults.Diagnostics.Any())
            {
                var messages = generatorResults.Diagnostics.Select(d => d.GetMessage());
                return new TemplateResult()
                {
                    GeneratedText = string.Empty,
                    ProcessingException = new TemplateProcessingException(messages, generatorResults.GeneratedCode)
                };
            }
            var templateResult = _compilationService.Compile(generatorResults.GeneratedCode);
            if (templateResult.Messages.Any())
            {
                return new TemplateResult()
                {
                    GeneratedText = string.Empty,
                    ProcessingException = new TemplateProcessingException(templateResult.Messages, generatorResults.GeneratedCode)
                };
            }

            var compiledObject = Activator.CreateInstance(templateResult.CompiledType);
            var razorTemplate = compiledObject as RazorTemplateBase;

            string result = String.Empty;
            if (razorTemplate != null)
            {
                razorTemplate.Model = templateModel;
                //ToDo: If there are errors executing the code, they are missed here.
                result = await razorTemplate.ExecuteTemplate();
            }

            return new TemplateResult()
            {
                GeneratedText = result,
                ProcessingException = null
            };

        }

        private class DefaultImportItem : RazorProjectItem
        {
            private readonly byte[] _defaultImportBytes;

            private DefaultImportItem()
            {
                var preamble = Encoding.UTF8.GetPreamble();
                var content = @"
@using System
@using System.Threading.Tasks
";
                var contentBytes = Encoding.UTF8.GetBytes(content);

                _defaultImportBytes = new byte[preamble.Length + contentBytes.Length];
                preamble.CopyTo(_defaultImportBytes, 0);
                contentBytes.CopyTo(_defaultImportBytes, preamble.Length);
            }

            public override string BasePath => null;

            public override string FilePath => null;

            public override string PhysicalPath => null;

            public override bool Exists => true;

            public static DefaultImportItem Instance { get; } = new DefaultImportItem();

            public override Stream Read() => new MemoryStream(_defaultImportBytes);
        }
    }
}