// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class ScaffoldingBuildProcessor : IMsBuildProcessor
    {
        private const string PackageDependencyResolverTarget = "ResolvePackageDependenciesDesignTime";
        private const string BuildTarget = "Build";
        private const string ResolveReferenceTarget = "ResolveReferences";
        private Dictionary<string, DependencyDescription> _packages;
        private IEnumerable<ResolvedReference> _resolvedReferences;
        private Project _project;
        private string _configuration;

        public ScaffoldingBuildProcessor(string configuration = "Debug")
        {
            ILogger logger = new Build.Logging.ConsoleLogger(LoggerVerbosity.Quiet);
            Loggers = new ILogger[] { logger };
            _configuration = configuration;
            Properties = new Dictionary<string, string>
            {
                { "Configuration", Configuration },
                { "GenerateDependencyFile", "true" },           // Generate deps.json
                { "GenerateRuntimeConfigurationFiles", "true"}, // Generate runtime.config.json
                { "DesignTimeBuild", "true" },
                { "AutoGenerateBindingRedirects", "true" }      // BindingRedirects
            };
        }

        public string Configuration
        {
            get
            {
                return _configuration;
            }
        }

        public IEnumerable<ILogger> Loggers { get; }

        public Dictionary<string, string> Properties { get; }

        public string[] Targets
        {
            get
            {
                return new string[]
                {
                    //BuildTarget,
                    PackageDependencyResolverTarget,
                    ResolveReferenceTarget
                };
            }
        }

        public NuGetFramework TargetFramework { get; private set; }
        public bool IsClassLibrary { get; private set; }
        public string Config { get; private set; }
        public string DepsJson { get; private set; }
        public string RuntimeConfigJson { get; private set; }
        public string PackagesDirectory { get; private set; }
        public string AssemblyFullPath { get; private set; }
        public string ProjectName { get; private set; }
        public string Platform { get; private set; }
        public string ProjectFullPath { get; private set; }
        public string RootNamespace { get; private set; }
        public string TargetDirectory { get; private set; }

        public IEnumerable<ResolvedReference> ResolvedReferences
        {
            get
            {
                return _resolvedReferences;
            }
        }

        public Dictionary<string, DependencyDescription> Packages
        {
            get
            {
                return _packages;
            }
        }

        public void Init()
        {
            // Do nothing for now.
        }

        public void ProcessBuildResult(Project project, ProjectInstance projectInstance, IDictionary<string, TargetResult> targetResults)
        {
            SetProjectProperties(projectInstance);
            _project = project;
            TargetResult packageDependencyResolutionResult = null;
            if (targetResults.TryGetValue(PackageDependencyResolverTarget, out packageDependencyResolutionResult))
            {
                ProcessDependencyResolutionResult(packageDependencyResolutionResult);
            }

            TargetResult resolveReferenceResult = null;
            if (targetResults.TryGetValue(ResolveReferenceTarget, out resolveReferenceResult))
            {
                ProcessReferenceResolutionResult(resolveReferenceResult, projectInstance);
            }
        }

        private void ProcessReferenceResolutionResult(TargetResult resolveReferenceResult, ProjectInstance projectInstance)
        {
            if (resolveReferenceResult.ResultCode == TargetResultCode.Success)
            {
                // The output of this target is in the projectItems with itemType "ReferencePath"
                _resolvedReferences = projectInstance
                    .Items
                    .Where(i => i != null && "ReferencePath".Equals(i.ItemType, StringComparison.OrdinalIgnoreCase))
                    .Select(i => BuildResolvedReference(i))
                    .Where(r => r!= null);
            }
            else
            {
                // Resolved References Target failed.
                throw new InvalidOperationException("Failed to obtain resolved references of the project. Make sure the project can build");
            }
        }

        private ResolvedReference BuildResolvedReference(ProjectItemInstance i)
        {
            return ProjectUtilities.CreateResolvedReferenceFromProjectItem(i);
        }

        private void ProcessDependencyResolutionResult(TargetResult result)
        {
            _packages = new Dictionary<string, DependencyDescription>();
            if (result.ResultCode == TargetResultCode.Success)
            {
                var items = result.Items;
                if (items != null && items.Any())
                {
                    var packageDependencies = items.Select(i => ProcessDependencyResolutionItem(i)).Where(p => p!= null);

                    foreach (var p in packageDependencies)
                    {
                        _packages[p.ItemSpec] = p;
                    }
                    // Need to make a 2nd pass on these items to populate all the dependencies.
                    PopulateDependencies(_packages, items);
                }
            }
            else
            {
                // TODO What to do here?
                throw new InvalidOperationException("Failed to retrieve the project dependencies. Make sure the project can be built!!");
            }
        }

        private void PopulateDependencies(Dictionary<string, DependencyDescription> dependencies, ITaskItem[] items)
        {
            foreach (var item in items)
            {
                var depSpecs = item.GetMetadata("Dependencies")
                    ?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                DependencyDescription current = null;
                if (depSpecs == null || !dependencies.TryGetValue(item.GetMetadata("ItemSpec"), out current))
                {
                    return;
                }

                foreach (var depSpec in depSpecs)
                {
                    var spec = item.GetMetadata("ItemSpec").Split('/').FirstOrDefault() + depSpec;
                    DependencyDescription d = null;
                    if (dependencies.TryGetValue(spec, out d))
                    {
                        current.AddDependency(new Dependency(d.Name, d.Version, d.ItemSpec));
                    }
                }
            }
        }

        private DependencyDescription ProcessDependencyResolutionItem(ITaskItem item)
        {
            return ProjectUtilities.CreateDependencyDescriptionFromTaskItem(item);
        }

        private void SetProjectProperties(ProjectInstance projectInstance)
        {
            ProjectName = Path.GetFileNameWithoutExtension(projectInstance.FullPath);
            ProjectFullPath = FindProperty(projectInstance, "ProjectPath");
            RootNamespace = FindProperty(projectInstance, "RootNamespace") ?? ProjectName;
            IsClassLibrary = FindProperty(projectInstance, "OutputType").Equals("Library", StringComparison.OrdinalIgnoreCase);
            Platform = FindProperty(projectInstance, "Platform");
            AssemblyFullPath = FindProperty(projectInstance, "TargetPath");
            Config = AssemblyFullPath + ".config";

            // The below are available only if the project is restored. 
            // Should this throw exception if restore is not run yet?
            TargetFramework = NuGetFramework.Parse(FindProperty(projectInstance, "NuGetTargetMoniker"));
            RuntimeConfigJson = FindProperty(projectInstance, "_ProjectRuntimeConfigFilePath");
            DepsJson = FindProperty(projectInstance, "_ProjectDepsFilePath");
            PackagesDirectory = FindProperty(projectInstance, "NuGetPackageRoot");
            TargetDirectory = FindProperty(projectInstance, "TargetDir");
        }

        private string FindProperty(ProjectInstance project, string propertyName)
             => project.Properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?.EvaluatedValue;

        public IMsBuildProjectContext CreateMsBuildProjectContext()
        {
            var rootProjectMsBuildFile = new MsBuildProjectFile(_project.FullPath,
                _project.GetItems("Compile").Select(i => i.EvaluatedInclude),
                _project.GetItems("ProjectReferences").Select(i => i.EvaluatedInclude),
                _project.GetItems("Reference").Select(i => i.EvaluatedInclude),
                _project.GlobalProperties
                );

            var dependencyProjectFiles = GetDependencyProjectFiles(_project);

            return new MsBuildProjectContext(ProjectName,
                Configuration,
                ProjectFullPath,
                rootProjectMsBuildFile,
                TargetFramework,
                Platform,
                Config,
                DepsJson,
                RootNamespace,
                dependencyProjectFiles);
        }

        private IEnumerable<MsBuildProjectFile> GetDependencyProjectFiles(Project _project)
        {
            List<MsBuildProjectFile> files = new List<MsBuildProjectFile>();

            //foreach (var proj in _project.GetItems("ProjectReferences"))
            //{
            //    var path = proj.EvaluatedInclude;
            //    if (!Path.IsPathRooted(path))
            //    {
            //        path = Path.Combine(_project.FullPath, path);
            //    }

            //    var dependencyProject = ProjectUtilities.CreateProject(path, _project.GlobalProperties);
            //}

            return files;
        }

        public IProjectDependencyProvider CreateDependencyProvider()
        {
            return new ProjectDependencyProvider(Packages, ResolvedReferences);
        }

    }

    
}
