// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Helpers.Extensions;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps.Project
{
    internal class ProjectDiscovery
    {
        public ProjectDiscovery(IFileSystem fileSystem, string workingDir)
        {
            FileSystem = fileSystem;
            WorkingDir = workingDir;
        }

        protected IFileSystem FileSystem { get; }
        protected string WorkingDir { get; set; }
        public FlowStepState State { get; private set; }
        public string? Discover(IFlowContext context, string path)
        {
            List<string> projects = []; 
            projects = AnsiConsole
                .Status()
                .WithSpinner()
                .Start("Discovering project files!", statusContext =>
                {
                    if (!FileSystem.DirectoryExists(path))
                    {
                        return [];
                    }

                    var projects = FileSystem.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories).ToList();
                    var slnFiles = FileSystem.EnumerateFiles(path, "*.sln", SearchOption.TopDirectoryOnly).ToList();
                    var projectsFromSlnFiles = GetProjectsFromSolutionFiles(slnFiles, WorkingDir);
                    projects = projects.Union(projectsFromSlnFiles).ToList();

                    //should we search in all directories if nothing in top level? (yes, for now)
                    if (projects.Count == 0)
                    {
                        projects = FileSystem.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories).ToList();
                    }

                    return projects;
                });

            return Prompt(context, $"[lightseagreen]Pick project ({projects.Count}): [/]", projects);
        }

        internal IList<string> GetProjectsFromSolutionFiles(List<string> solutionFiles, string workingDir)
        {
            List<string> projectPaths = new();
            foreach(var solutionFile in solutionFiles)
            {
                projectPaths.AddRange(GetProjectsFromSolutionFile(solutionFile, workingDir));
            }

            return projectPaths;
        }

        internal IList<string> GetProjectsFromSolutionFile(string solutionFilePath, string workingDir)
        {
            List<string> projectPaths = new();
            if (string.IsNullOrEmpty(solutionFilePath))
            {
                return projectPaths;
            }

            string slnText = FileSystem.ReadAllText(solutionFilePath);
            string projectPattern = @"Project\(""\{[A-F0-9\-]*\}""\)\s*=\s*""([^""]*)"",\s*""([^""]*)"",\s*""\{[A-F0-9\-]*\}""";
            var matches = Regex.Matches(slnText, projectPattern);
            foreach (Match match in matches)
            {
                string projectRelativePath = match.Groups[2].Value;
                string? solutionDirPath = Path.GetDirectoryName(solutionFilePath) ?? workingDir;
                string projectPath = Path.GetFullPath(projectRelativePath, solutionDirPath);
                projectPaths.Add(projectPath);
            }

            return projectPaths;
        }

        internal string GetProjectDisplayName(string projectPath)
        {
            var name = Path.GetFileNameWithoutExtension(projectPath);
            var relativePath = projectPath.MakeRelativePath(WorkingDir).ToSuggestion();
            return $"{name} {relativePath.ToSuggestion(withBrackets: true)}";
        }

        private string? Prompt(IFlowContext context, string title, IList<string> projectFiles)
        {
            if (projectFiles.Count == 0)
            {
                return null;
            }

            if (projectFiles.Count == 1)
            {
                return projectFiles[0];
            }

            var prompt = new FlowSelectionPrompt<string>()
                .Title(title)
                .Converter(GetProjectDisplayName)
                .AddChoices(projectFiles.OrderBy(x => Path.GetFileNameWithoutExtension(x)), navigation: context.Navigation);

            var result = prompt.Show();
            State = result.State;
            return result.Value;
        }
    }

}

