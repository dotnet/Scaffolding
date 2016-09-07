using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class RoslynWorkspace : Workspace
    {
        private Dictionary<string, AssemblyMetadata> _cache = new Dictionary<string, AssemblyMetadata>();
        private HashSet<string> _projectReferences = new HashSet<string>();
        //MsBuildProjectContext _context;
        public RoslynWorkspace(MsBuildProjectContext context, string configuration = "debug") : base(MefHostServices.DefaultHost, "Custom")
        {
            var id = AddProject(context, configuration);
            AddMetadataReferences(context, id);
        }

        private void AddMetadataReferences(MsBuildProjectContext context, ProjectId id)
        {
            var libraryExporter = context.CreateLibraryExporter();
            var exports = libraryExporter.GetExports() ?? new List<Library>();
            
            foreach(var export in exports)
            {
                if(!_projectReferences.Contains(export.Name))
                {
                    var compLibrary = export as CompilationLibrary;
                    if(compLibrary != null)
                    {
                        // TODO How to get resolved path ?
                        OnMetadataReferenceAdded(id, GetMetadataReference(""));
                    }
                }
            }
        }

        private ProjectId AddProject(MsBuildProjectContext context, string configuration)
        {
            var projectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                context.ProjectName,
                context.ProjectName,
                LanguageNames.CSharp,
                context.ProjectFullPath);

            _projectReferences.Add(context.ProjectName);

            OnProjectAdded(projectInfo);

            foreach (var file in context.ProjectFile.SourceFiles)
            {
                var filePath = Path.IsPathRooted(file.EvaluatedInclude)
                    ? file.EvaluatedInclude :
                    Path.Combine(Path.GetDirectoryName(context.ProjectFullPath), file.EvaluatedInclude);
                AddSourceFile(projectInfo, filePath);
            }

            //TODO: Look at project dependencies, and add metadata references
            // Also need to figure out how to add the shared sources.

            foreach(var project in context.ProjectFile.ProjectReferences)
            {
                var projPath = project.EvaluatedInclude;
                if (!Path.IsPathRooted(projPath))
                {
                    Path.Combine(Path.GetDirectoryName(context.ProjectFullPath), projPath);
                }
                var id = AddProject(new MsBuildProjectContext(projPath, configuration), configuration);
                OnProjectReferenceAdded(id, new ProjectReference(id));
            }

            return projectInfo.Id;
        }

        private void AddSourceFile(ProjectInfo projectInfo, string file)
        {
            if(!File.Exists(file))
            {
                return;
            }

            using (var stream = File.OpenRead(file))
            {
                var sourceText = SourceText.From(stream, Encoding.UTF8);
                var id = DocumentId.CreateNewId(projectInfo.Id);
                var version = VersionStamp.Create();

                var loader = TextLoader.From(TextAndVersion.Create(sourceText, version));
                OnDocumentAdded(DocumentInfo.Create(id, file, filePath: file, loader: loader));
            }
        }

        private MetadataReference GetMetadataReference(string path)
        {
            AssemblyMetadata assemblyMetadata;
            if (!_cache.TryGetValue(path, out assemblyMetadata))
            {
                using (var stream = File.OpenRead(path))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                    assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    _cache[path] = assemblyMetadata;
                }
            }

            return assemblyMetadata.GetReference();
        }
    }
}
