// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation
{
    public class CompilationResult
    {
        private readonly Type _type;

        private CompilationResult(string generatedCode, Type type, IEnumerable<string> messages)
        {
            _type = type;
            GeneratedCode = generatedCode;
            Messages = messages;
        }

        public IEnumerable<string> Messages { get; private set; }

        public string GeneratedCode { get; private set; }

        public Type CompiledType
        {
            get
            {
                if (_type == null)
                {
                    throw new TemplateProcessingException(Messages, GeneratedCode);
                }

                return _type;
            }
        }

        public static CompilationResult Failed(string generatedCode, IEnumerable<string> messages)
        {
            return new CompilationResult(generatedCode, type: null, messages: messages);
        }

        public static CompilationResult Successful(string generatedCode, Type type)
        {
            return new CompilationResult(generatedCode, type, Enumerable.Empty<string>());
        }
    }
}
