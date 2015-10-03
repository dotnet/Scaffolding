// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.CodeGeneration.Templating.Compilation
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
