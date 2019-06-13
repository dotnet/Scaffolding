// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    public abstract class RazorTemplateBase
    {
        private TextWriter Output { get; set; }

        public dynamic Model { get; set; }

        public abstract Task ExecuteAsync();

        public async Task<string> ExecuteTemplate()
        {
            StringBuilder output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                Output = writer;
                await ExecuteAsync();
            }
            return output.ToString();
        }

        public void WriteLiteral(object value)
        {
            WriteLiteralTo(Output, value);
        }

        public virtual void WriteLiteralTo(TextWriter writer, object text)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (text != null)
            {
                writer.Write(text.ToString());
            }
        }

        public virtual void Write(object value)
        {
            WriteTo(Output, value);
        }

        public virtual void WriteTo(TextWriter writer, object content)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (content != null)
            {
                writer.Write(content.ToString());
            }
        }

        public void BeginContext(int position, int length, bool x)
        {
            // Do Nothing.
        }

        public void EndContext()
        {
            // Do Nothing.
        }

        private List<string> AttributeValues { get; set; }

        protected void WriteAttributeValue(string thingy, int startPostion, object value, int endValue, int dealyo, bool yesno)
        {
            if (AttributeValues == null)
            {
                AttributeValues = new List<string>();
            }

            AttributeValues.Add(value.ToString());
        }

        private string AttributeEnding { get; set; }

        // Copied from: https://github.com/aspnet/AspNetCore/blob/master/src/Shared/RazorViews/BaseView.cs
        protected void BeginWriteAttribute(string name, string begining, int startPosition, string ending, int endPosition, int thingy)
        {
            Debug.Assert(string.IsNullOrEmpty(AttributeEnding));

            Output.Write(begining);
            AttributeEnding = ending;
        }

        protected void EndWriteAttribute()
        {
            Debug.Assert(!string.IsNullOrEmpty(AttributeEnding));

            if (AttributeValues != null)
            {
                var attributes = string.Join(" ", AttributeValues);
                Output.Write(attributes);
            }

            AttributeValues = null;

            Output.Write(AttributeEnding);
            AttributeEnding = null;
        }
    }
}
