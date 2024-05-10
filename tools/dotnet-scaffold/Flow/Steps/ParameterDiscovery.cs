// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Extensions;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ParameterDiscovery
    {
        private readonly Parameter _parameter;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentService _environmentService;
        public ParameterDiscovery(
            Parameter parameter,
            IFileSystem fileSystem,
            IEnvironmentService environmentService)
        {
            _parameter = parameter;
            _fileSystem = fileSystem;
            _environmentService = environmentService;
        }
        public FlowStepState State { get; private set; }

        public async Task<string> DiscoverAsync(IFlowContext context)
        {
            var optionParameterAddition = _parameter.Required ? "(" : "(empty to skip, ";
            return await PromptAsync(context, $"Enter new value for '{_parameter.DisplayName}' {optionParameterAddition}[sandybrown]<[/] to go back) : ");
        }

        private async Task<string> PromptAsync(IFlowContext context, string title)
        {
            //check if Parameter has a InteractivePickerType
            if (_parameter.PickerType is null)
            {
                var prompt = new TextPrompt<string>($"[lightseagreen]{title}[/]")
                .ValidationErrorMessage("bad value fix it please")
                .Validate(x =>
                {
                    if (x.Trim() == FlowNavigation.BackInputToken)
                    {
                        return ValidationResult.Success();
                    }

                    return Validate(context, x);
                })
                .AllowEmpty();

                await Task.Delay(1);
                return AnsiConsole.Prompt(prompt).Trim();
            }
            else
            {
                return await PromptInteractivePicker(context, _parameter.PickerType) ?? string.Empty;
            }
        }

        private async Task<string?> PromptInteractivePicker(IFlowContext context, InteractivePickerType? pickerType)
        {
            IList<Tuple<string, string>>? displayTuples = [];
            string interactiveTitle = string.Empty;
            switch (pickerType)
            {
                case InteractivePickerType.ClassPicker:
                    var allClassSymbols = await AnsiConsole
                    .Status()
                    .WithSpinner()
                    .Start("Gathering project classes!", async statusContext =>
                    {
                        //ICodeService might be null if no InteractivePickerType.ProjectPicker was passed.
                        //will add better documentation so users will know what to expect.
                        var codeService = context.GetCodeService();
                        if (codeService is null)
                        {
                            return [];
                        }

                        return (await codeService.GetAllClassSymbolsAsync()).ToList();
                    });

                    displayTuples = GetClassDisplayNames(allClassSymbols);
                    interactiveTitle = "Class";
                    break;
                case InteractivePickerType.FilePicker:
                    //ICodeService might be null if no InteractivePickerType.ProjectPicker was passed.
                    //will add better documentation so users will know what to expect.
                    var codeService = context.GetCodeService();
                    if (codeService is null)
                    {
                        displayTuples = [];
                    }
                    else
                    {
                        var allDocuments = (await codeService.GetAllDocumentsAsync()).ToList();
                        displayTuples = GetDocumentNames(allDocuments);
                    }

                    interactiveTitle = "File";
                    break;
                case InteractivePickerType.DbProviderPicker:
                    displayTuples = DbProviders;
                    interactiveTitle = "DbProvider";
                    break;
                case InteractivePickerType.ProjectPicker:
                    displayTuples = GetProjectFiles();
                    interactiveTitle = "Project";
                    break;
            }

            if (!_parameter.Required)
            {
                displayTuples.Insert(0, Tuple.Create("None", string.Empty));
            }

            var prompt = new FlowSelectionPrompt<Tuple<string, string>>()
                .Title($"[lightseagreen]Pick a {interactiveTitle}: [/]")
                .Converter(GetDisplayNameFromTuple)
                .AddChoices(displayTuples, navigation: context.Navigation);

            var result = prompt.Show();
            State = result.State;
            return result.Value?.Item2;
        }

        private string GetDisplayNameFromTuple(Tuple<string, string> tuple)
        {
            bool displayNone = tuple.Item1.Equals("None", StringComparison.OrdinalIgnoreCase);
            return displayNone ? $"[sandybrown]{tuple.Item1} (empty to skip parameter)[/]" : $"{tuple.Item1} ({tuple.Item2})";
        }

        private ValidationResult Validate(IFlowContext context, string promptVal)
        {
            if (!_parameter.Required && string.IsNullOrEmpty(promptVal))
            {
                return ValidationResult.Success();
            }

            if (!string.IsNullOrEmpty(promptVal) && !ParameterHelpers.CheckType(_parameter.Type, promptVal))
            {
                return ValidationResult.Error("Invalid input, please try again!");
            }

            return ValidationResult.Success();
        }

        private List<Tuple<string, string>> GetClassDisplayNames(List<ISymbol> compilationClassSymbols)
        {
            List<Tuple<string, string>> classNames = [];
            if (compilationClassSymbols != null && compilationClassSymbols.Count != 0)
            {
                compilationClassSymbols.ForEach(
                x =>
                {
                    if (x != null)
                    {
                        classNames.Add(Tuple.Create(x.MetadataName, x.Name));
                    }
                });
            }

            return classNames;
        }

        private List<Tuple<string, string>> GetDocumentNames(List<Document> documents)
        {
            List<Tuple<string, string>> classNames = [];
            if (documents != null && documents.Count != 0)
            {
                documents.ForEach(
                x =>
                {
                    if (x != null)
                    {
                        string fileName = System.IO.Path.GetFileName(x.Name);
                        classNames.Add(Tuple.Create(fileName, x.Name));
                    }
                });
            }

            return classNames;
        }

        internal List<Tuple<string, string>> GetProjectFiles()
        {
            var workingDirectory = _environmentService.CurrentDirectory;
            if (!Path.IsPathRooted(workingDirectory))
            {
                workingDirectory = Path.GetFullPath(Path.Combine(_environmentService.CurrentDirectory, workingDirectory.Trim(Path.DirectorySeparatorChar)));
            }
            if (!_fileSystem.DirectoryExists(workingDirectory))
            {
                return [];
            }

            var projects = _fileSystem.EnumerateFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories).ToList();
            var slnFiles = _fileSystem.EnumerateFiles(workingDirectory, "*.sln", SearchOption.TopDirectoryOnly).ToList();
            var projectsFromSlnFiles = GetProjectsFromSolutionFiles(slnFiles, workingDirectory);
            projects = projects.Union(projectsFromSlnFiles).ToList();

            //should we search in all directories if nothing in top level? (yes, for now)
            if (projects.Count == 0)
            {
                projects = _fileSystem.EnumerateFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories).ToList();
            }
            
            return projects.Select(x => Tuple.Create(GetProjectDisplayName(x, workingDirectory), x)).ToList();
        }

        internal IList<string> GetProjectsFromSolutionFiles(List<string> solutionFiles, string workingDir)
        {
            List<string> projectPaths = new();
            foreach (var solutionFile in solutionFiles)
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

            string slnText = _fileSystem.ReadAllText(solutionFilePath);
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

        internal string GetProjectDisplayName(string projectPath, string workingDir)
        {
            var name = Path.GetFileNameWithoutExtension(projectPath);
            var relativePath = projectPath.MakeRelativePath(workingDir).ToSuggestion();
            return $"{name} {relativePath.ToSuggestion(withBrackets: true)}";
        }

        private static List<Tuple<string, string>>? _dbProviders;
        private static List<Tuple<string, string>> DbProviders
        {
            get
            {
                _dbProviders ??=
                [
                    Tuple.Create("SQL Server", "sqlserver"),
                    Tuple.Create("SQLite", "sqlite"),
                    Tuple.Create("PostgreSQL", "postgres"),
                    Tuple.Create("Cosmos DB", "cosmos")
                ];

                return _dbProviders;
            }
        }
    }
}
