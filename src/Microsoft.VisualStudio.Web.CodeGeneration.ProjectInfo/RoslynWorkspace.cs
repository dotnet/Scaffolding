using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using PInfo = Microsoft.CodeAnalysis.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class RoslynWorkspace : Workspace
    {
        private Dictionary<string, AssemblyMetadata> _cache = new Dictionary<string, AssemblyMetadata>();

        public RoslynWorkspace(MsBuildProjectContext context,
            ProjectDependencyProvider projectDependencyProvider,
            string configuration = "debug")
            : base(MefHostServices.DefaultHost, "Custom")
        {
            Requires.NotNull(context);
            Requires.NotNull(projectDependencyProvider);

            var id = AddProject(context.ProjectFile, configuration);

            // Since we have resolved all references, we can directly use them as MetadataReferences.
            // Trying to get ProjectReferences manually might lead to problems when the projects have circular dependency.

            //foreach (var file in context.DependencyProjectFiles)
            //{
            //    AddProjectReference(file, configuration);
            //}

            AddMetadataReferences(projectDependencyProvider, id);
        }

        private ProjectId AddProject(MsBuildProjectFile projectFile, string configuration)
        {
            var fullPath = projectFile.FullPath;

            var projectName = Path.GetFileNameWithoutExtension(fullPath);
            var projectInfo = PInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                projectName,
                projectName,
                LanguageNames.CSharp,
                fullPath);

            OnProjectAdded(projectInfo);

            foreach (var file in projectFile.SourceFiles)
            {
                var filePath = Path.IsPathRooted(file)
                    ? file :
                    Path.Combine(Path.GetDirectoryName(fullPath), file);
                AddSourceFile(projectInfo, filePath);
            }

            return projectInfo.Id;
        }

        private void AddSourceFile(PInfo projectInfo, string file)
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

        private void AddMetadataReferences(ProjectDependencyProvider projectDependencyProvider, ProjectId id)
        {
            var resolvedReferences = projectDependencyProvider.GetAllResolvedReferences();

            foreach (var reference in resolvedReferences)
            {
                if (!reference.IsResolved)
                {
                    continue;
                }

                var metadataRef = GetMetadataReference(reference.ResolvedPath);
                if (metadataRef != null)
                {
                    OnMetadataReferenceAdded(id, metadataRef);
                }
            }
        }

        private MetadataReference GetMetadataReference(string assetPath)
        {
            var extension = Path.GetExtension(assetPath);

            string path = assetPath;
            if (string.IsNullOrEmpty(extension) || !ValidExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var ext in ValidExtensions)
                {
                    path = assetPath + ext;
                    if (File.Exists(path))
                    {
                        break;
                    }
                }
            }

            AssemblyMetadata assemblyMetadata = null;
            if (!_cache.TryGetValue(path, out assemblyMetadata))
            {
                if (File.Exists(path))
                {
                    using (var stream = File.OpenRead(path))
                    {
                        var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                        assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                        _cache[path] = assemblyMetadata;
                    }
                }
            }

            return assemblyMetadata?.GetReference();
        }

        private static List<string> ValidExtensions = new List<string>()
        {
            ".dll",
            ".exe"
        };
    }
}
