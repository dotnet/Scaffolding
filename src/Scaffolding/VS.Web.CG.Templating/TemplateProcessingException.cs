// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    public class TemplateProcessingException : Exception
    {
        public TemplateProcessingException(IEnumerable<string> messages, string generatedCode)
            : base(FormatMessage(messages))
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            Messages = messages;
            GeneratedCode = generatedCode;
        }

        public string GeneratedCode { get; private set; }

        public IEnumerable<string> Messages { get; private set; }

        public override string Message
        {
            get
            {
                return string.Format(MessageStrings.TemplateProcessingError, FormatMessage(Messages));
            }
        }

        private static string FormatMessage(IEnumerable<string> messages)
        {
            return String.Join(Environment.NewLine, messages);
        }
    }
}
