// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution;
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
            var razorEngine = RazorEngine.Create();

            // Don't care about the RazorProject as we already have the content of the .cshtml file 
            // and don't need to deal with imports.
            var razorProject = new FileSystemRazorProject(Directory.GetCurrentDirectory());
            var razorTemplateEngine = new RazorTemplateEngine(razorEngine, razorProject);

            var razorDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(content, string.Empty));
            var generatorResults = razorTemplateEngine.GenerateCode(razorDocument);

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
    }
}