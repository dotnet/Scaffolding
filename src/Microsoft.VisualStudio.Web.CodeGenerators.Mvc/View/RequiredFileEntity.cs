using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class RequiredFileEntity
    {
        public RequiredFileEntity(string outputPath, string templateName)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            TemplateName = templateName;
            OutputPath = outputPath;
        }
        
        /// <summary>
        /// Name of the template file.
        /// </summary>
        public string TemplateName { get; private set; }
        
        /// <summary>
        /// Path Relative to the project.json.
        /// </summary>
        public string OutputPath { get; private set; }
    }
}
