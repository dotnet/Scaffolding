// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating.Test
{
    public class TestCompilationService : ICompilationService
    {
        public Compilation.CompilationResult Compile(string content)
        {
            var loader = new DefaultAssemblyLoadContext();
            var projectContext = CreateProjectContext(null);
            var libraryExporter = projectContext.CreateExporter("Debug");

            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };

            var exports = libraryExporter.GetAllExports();

            var references = GetMetadataReferences(exports);
            var assemblyName = Path.GetRandomFileName();


            var compilation = CSharpCompilation.Create(assemblyName,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees: syntaxTrees,
            references: references);

            var result = CommonUtilities.GetAssemblyFromCompilation(loader, compilation);
            if (result.Success)
            {
                var type = result.Assembly.GetExportedTypes()
                                   .First();

                return Templating.Compilation.CompilationResult.Successful(string.Empty, type);
            }
            else
            {
                return Templating.Compilation.CompilationResult.Failed(content, result.ErrorMessages);
            }
        }

        private List<MetadataReference> GetMetadataReferences(IEnumerable<LibraryExport> exports)
        {
            var references = new List<MetadataReference>();

            foreach (var exp in exports)
            {
                foreach (var lib in exp.CompilationAssemblies)
                {
                    var metadataReference = GetMetadataReference(lib.ResolvedPath);
                    if (metadataReference != null)
                    {
                        references.Add(metadataReference);
                    }
                }
            }

            return references;
        }

        private MetadataReference GetMetadataReference(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                    var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    return assemblyMetadata.GetReference();
                }
            }
            catch
            {
                return null;
            }
        }

        private static ProjectContext CreateProjectContext(string projectPath)
        {
            projectPath = projectPath ?? Directory.GetCurrentDirectory();
            var framework = NuGet.Frameworks.FrameworkConstants.CommonFrameworks.NetCoreApp10.GetShortFolderName();
            if (!projectPath.EndsWith(Microsoft.DotNet.ProjectModel.Project.FileName))
            {
                projectPath = Path.Combine(projectPath, Microsoft.DotNet.ProjectModel.Project.FileName);
            }

            if (!File.Exists(projectPath))
            {
                throw new InvalidOperationException($"{projectPath} does not exist.");
            }
            return ProjectContext.CreateContextForEachFramework(projectPath).FirstOrDefault(c => c.TargetFramework.GetShortFolderName() == framework);
        }
    }
}
