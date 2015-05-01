// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor;
using Microsoft.Framework.CodeGeneration.Templating.Compilation;

namespace Microsoft.Framework.CodeGeneration.Templating
{
    //Todo: Make this internal and expose as a sevice
    public class RazorTemplating : ITemplating
    {
        private ICompilationService _compilationService;

        public RazorTemplating([NotNull]ICompilationService compilationService)
        {
            _compilationService = compilationService;
        }

        public async Task<TemplateResult> RunTemplateAsync(string content,
            dynamic templateModel)
        {
            RazorTemplatingHost host = new RazorTemplatingHost(typeof(RazorTemplateBase));
            RazorTemplateEngine engine = new RazorTemplateEngine(host);

            using (var reader = new StringReader(content))
            {
                var generatorResults = engine.GenerateCode(reader);

                if (!generatorResults.Success)
                {
                    var messages = generatorResults.ParserErrors.Select(e => e.Message);
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
}