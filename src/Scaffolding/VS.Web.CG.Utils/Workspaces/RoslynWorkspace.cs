// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using PInfo = Microsoft.CodeAnalysis.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils
{
    public class RoslynWorkspace : Workspace
    {
        private static List<string> ValidExtensions = new List<string>()
        {
            ".dll",
            ".exe"
        };

        private Dictionary<string, AssemblyMetadata> _cache = new Dictionary<string, AssemblyMetadata>();

        public RoslynWorkspace(IProjectContext projectInformation,
            string configuration = "debug")
            : base(MefHostServices.DefaultHost, "Custom")
        {
            Requires.NotNull(projectInformation, nameof(projectInformation));

            var id = AddProject(projectInformation, configuration);

            // Since we have resolved all references, we can directly use them as MetadataReferences.
            // Trying to get ProjectReferences manually might lead to problems when the projects have circular dependency.
            if (projectInformation.ProjectReferenceInformation != null)
            {
                foreach (var projectReference in projectInformation.ProjectReferenceInformation)
                {
                    AddProject(projectReference, configuration);
                }
            }

            AddMetadataReferences(projectInformation, id);
        }

        private ProjectId AddProject(ProjectReferenceInformation referenceInfo, string configuration)
        {
            var fullPath = referenceInfo.FullPath;

            var projectInfo = PInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                referenceInfo.ProjectName,
                referenceInfo.AssemblyName,
                LanguageNames.CSharp,
                fullPath);

            OnProjectAdded(projectInfo);

            foreach(var file in referenceInfo.CompilationItems)
            {
                var filePath = Path.IsPathRooted(file)
                    ? file
                    : Path.Combine(Path.GetDirectoryName(fullPath), file);
                AddSourceFile(projectInfo, filePath);
            }

            return projectInfo.Id;
        }

        private ProjectId AddProject(IProjectContext context, string configuration)
        {
            var fullPath = context.ProjectFullPath;

            var projectInfo = PInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                context.ProjectName,
                context.AssemblyName,
                LanguageNames.CSharp,
                fullPath);

            OnProjectAdded(projectInfo);

            foreach (var file in context.CompilationItems)
            {
                var filePath = Path.IsPathRooted(file)
                    ? file
                    : Path.Combine(Path.GetDirectoryName(fullPath), file);
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

        private void AddMetadataReferences(IProjectContext projectContext, ProjectId id)
        {
            var packages = projectContext.PackageDependencies;
            var resolvedReferences = projectContext.CompilationAssemblies;

            foreach (var reference in resolvedReferences)
            {
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
    }
}
