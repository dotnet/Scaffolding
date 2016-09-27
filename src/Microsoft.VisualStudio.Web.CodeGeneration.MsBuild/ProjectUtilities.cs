using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class ProjectUtilities
    {
        public static Project CreateProject(string filePath, string configuration, IDictionary<string, string> globalProperties)
        {
            var xmlReader = XmlReader.Create(new FileStream(filePath, FileMode.Open));
            var projectCollection = new ProjectCollection();
            var xml = ProjectRootElement.Create(xmlReader, projectCollection);
            xml.FullPath = filePath;

            var project = new Project(xml, globalProperties, /*toolsVersion*/ null, projectCollection);
            return project;
        }
    }
}
