using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Scaffolding.Shared.T4Templating
{
    /// <summary>
    /// Contains useful helper functions for running visual studio text transformation.
    /// For internal microsoft use only. Use <see cref="ITemplateInvoker"/>
    /// in custom code generators.
    /// </summary>
    public class TemplateInvoker : ITemplateInvoker
    {
        private readonly ConsoleLogger _consoleLogger;
        /// <summary>
        /// Constructor.
        /// </summary>
        public TemplateInvoker(ConsoleLogger consoleLogger = null)
        {
            _consoleLogger = consoleLogger ?? new ConsoleLogger();
        }

        /// <summary>
        /// Executes a code generator template to generate the code.
        /// </summary>
        /// <param name="template">ITextTransformation template object</param>
        /// <param name="templateParameters">Parameters for the template.
        /// These parameters can be accessed in text template using a parameter directive.
        /// The values passed in must be either serializable or 
        /// extend <see cref="MarshalByRefObject"/> type.</param>
        /// <returns>Generated code if there were no processing errors. Throws 
        /// <see cref="InvalidOperationException" /> otherwise.
        /// </returns>
        public string InvokeTemplate(ITextTransformation template, IDictionary<string, object> templateParameters)
        {
            if (template is null)
            {
                return string.Empty;
            }

            if (templateParameters is not null && templateParameters.Any())
            {
                foreach (var param in templateParameters)
                {
                    template.Session.Add(param.Key, param.Value);
                }
            }

            template.Initialize();
            return ProcessTemplate(template);
        }

        private string ProcessTemplate(ITextTransformation transformation)
        {
            var output = transformation.TransformText();
            if (transformation.Errors.HasErrors)
            {
                foreach (CompilerError error in transformation.Errors)
                {
                    _consoleLogger.LogMessage(error.ErrorText, LogMessageLevel.Error);
                }

                throw new InvalidOperationException($"Processing '{transformation.GetType().Name}' failed");
            }

            return output;
        }
    }
}
