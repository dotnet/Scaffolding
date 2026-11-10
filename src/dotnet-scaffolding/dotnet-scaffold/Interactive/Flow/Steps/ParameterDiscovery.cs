// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.Extensions;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Spectre.Console;
using Spectre.Console.Flow;
using Spectre.Console.Rendering;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps
{
    /// <summary>
    /// Handles the discovery and user input for a parameter, including interactive pickers and validation.
    /// Supports various picker types such as class, file, project, yes/no, and custom pickers.
    /// </summary>
    internal class ParameterDiscovery
    {
        private readonly Parameter _parameter;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentService _environmentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterDiscovery"/> class.
        /// </summary>
        public ParameterDiscovery(
            Parameter parameter,
            IFileSystem fileSystem,
            IEnvironmentService environmentService)
        {
            _parameter = parameter;
            _fileSystem = fileSystem;
            _environmentService = environmentService;
        }
        /// <summary>
        /// Gets the state of the flow step after execution.
        /// </summary>
        public FlowStepState State { get; private set; }

        /// <summary>
        /// Discovers the value for the parameter, prompting the user as needed.
        /// </summary>
        /// <param name="context">The flow context.</param>
        /// <returns>The discovered value as a string.</returns>
        public async Task<string> DiscoverAsync(IFlowContext context)
        {
            var optionParameterAddition = _parameter.Required ? "(" : "([purple]empty to skip[/],";

            return await PromptAsync(context, $"Enter a new value for '{_parameter.DisplayName}' {optionParameterAddition}[sandybrown]<[/] to go back) : ");
        }

        /// <summary>
        /// Prompts the user for input, using the appropriate picker or text prompt.
        /// </summary>
        private async Task<string> PromptAsync(IFlowContext context, string title)
        {
            // Check if Parameter has an InteractivePickerType
            if (_parameter.PickerType is InteractivePickerType.None)
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
                });

                if (!_parameter.Required)
                {
                    prompt = prompt.AllowEmpty();
                }

                await Task.Delay(1);
                return AnsiConsole.Prompt(prompt).Trim();
            }
            else
            {
                return await PromptInteractivePicker(context, _parameter.PickerType) ?? string.Empty;
            }
        }

        /// <summary>
        /// Prompts the user using an interactive picker based on the parameter's picker type.
        /// </summary>
        private async Task<string?> PromptInteractivePicker(IFlowContext context, InteractivePickerType? pickerType)
        {
            List<StepOption> stepOptions = [];
            var codeService = context.GetCodeService();
            var converter = GetDisplayName;
            switch (pickerType)
            {
                case InteractivePickerType.ClassPicker:
                    // ICodeService might be null if no InteractivePickerType.ProjectPicker was passed.
                    if (codeService is null)
                    {
                        stepOptions = [];
                    }
                    else
                    {
                        stepOptions = await AnsiConsole
                            .Status()
                            .WithSpinner()
                            .StartAsync("Finding model classes", _ => GetClassDisplayNamesAsync(codeService, context.GetCommandObj()));
                    }
                    break;
                case InteractivePickerType.FilePicker:
                    if (codeService is null)
                    {
                        stepOptions = [];
                    }
                    else
                    {
                        var allDocuments = await codeService.GetAllDocumentsAsync();
                        stepOptions = GetDocumentNames(allDocuments);
                    }
                    break;
                case InteractivePickerType.ProjectPicker:
                    stepOptions = GetProjectFiles();
                    converter = GetDisplayNameForProjects;
                    break;
                case InteractivePickerType.YesNo:
                    stepOptions = [new() { Name = "Yes", Value = "true" }, new() { Name = "No", Value = "false" }];
                    converter = GetDisplayNameForYesNo;
                    break;
                case InteractivePickerType.CustomPicker:
                    stepOptions = GetCustomValues(_parameter.CustomPickerValues);
                    break;
                case InteractivePickerType.DynamicPicker:
                    stepOptions = GetDynamicValues(context, _parameter);

                    if (AzCliHelper.GetAzCliErrors(context) is string azCliError && !string.IsNullOrEmpty(azCliError))
                    {
                        AnsiConsole.MarkupLine($"[red]Error with EntraID scaffolding in az cli environment: {azCliError}[/]");
                        State = FlowStepState.Exit;
                        return null;
                    }
                    else if (stepOptions.Count == 0)
                    {
                        throw new InvalidOperationException("Missing values from az CLI!.");
                    }
                    break;
                case InteractivePickerType.ConditionalPicker:
                    var affirmative = _parameter.CustomPickerValues?.FirstOrDefault("");
                    var negative = _parameter.CustomPickerValues?.LastOrDefault("");
                    if (!string.IsNullOrEmpty(affirmative) && !string.IsNullOrEmpty(negative))
                    {
                        stepOptions = [new() { Name = affirmative, Value = "true" }, new() { Name = negative, Value = "false" }];
                        converter = GetDisplayNameForYesNo;
                    }
                    break;
            }

            if (ShouldAddNoneOption(pickerType, stepOptions))
            {
                stepOptions.Insert(0, new StepOption() { Name = "None", Value = string.Empty });
            }

            var prompt = CreateSelectionPrompt(stepOptions, context.Navigation, converter);

            var result = prompt.Show();
            State = result.State;
            return result.Value?.Value;
        }

        internal FlowSelectionPrompt<StepOption> CreateSelectionPrompt(
            IEnumerable<StepOption> stepOptions,
            IFlowNavigation navigation,
            Func<StepOption, string>? converter = null,
            IAnsiConsole? console = null)
        {
            console ??= AnsiConsole.Console;
            converter ??= GetDisplayName;
            var choices = stepOptions.ToList();
            var title = $"[lightseagreen]{_parameter.DisplayName}: [/]";
            var prompt = new FlowSelectionPrompt<StepOption>()
                .Title(title)
                .Converter(converter)
                .AddChoices(choices, navigation: navigation);

            var renderOptions = new RenderOptions(console.Profile.Capabilities, new Size(console.Profile.Width, console.Profile.Height));
            int CountRows(string markup, int width) =>
                Segment.SplitLines(((IRenderable)new Markup(markup)).Render(renderOptions, Math.Max(1, width))).Count;

            // Spectre budgets choices, not wrapped rows; overflowing the screen crops the selected first choice.
            int choiceHeight = choices.Select(option => CountRows("> " + converter(option), console.Profile.Width - 1))
                .DefaultIfEmpty(1)
                .Max();
            int reservedRows = CountRows(title, console.Profile.Width) +
                CountRows(prompt.Raw.MoreChoicesText!, console.Profile.Width) + 2;
            int maxChoiceRows = Math.Max(1, (console.Profile.Height - reservedRows) / 3);
            if (choiceHeight > maxChoiceRows)
            {
                // Spectre requires at least three choices; shorten display labels, not their values.
                var originalConverter = converter;
                converter = option =>
                {
                    var markup = originalConverter(option);
                    if (CountRows("> " + markup, console.Profile.Width - 1) <= maxChoiceRows)
                    {
                        return markup;
                    }

                    var label = markup.RemoveMarkup();
                    var textElements = StringInfo.ParseCombiningCharacters(label);
                    string Shorten(int count) =>
                        Markup.Escape(label[..textElements[(count + 1) / 2]] + "..." +
                            (count / 2 == 0 ? string.Empty : label[textElements[^(count / 2)]..]));

                    int low = 0;
                    int high = textElements.Length - 1;
                    while (low < high)
                    {
                        int retained = (low + high + 1) / 2;
                        if (CountRows("> " + Shorten(retained), console.Profile.Width - 1) <= maxChoiceRows)
                        {
                            low = retained;
                        }
                        else
                        {
                            high = retained - 1;
                        }
                    }

                    return Shorten(low);
                };
                prompt.Converter(converter);
                choiceHeight = choices.Select(option => CountRows("> " + converter(option), console.Profile.Width - 1)).Max();
            }
            int pageSize = Math.Clamp((console.Profile.Height - reservedRows) / Math.Max(1, choiceHeight), 3, prompt.Raw.PageSize);

            return prompt.PageSize(pageSize);
        }

        /// <summary>
        /// Determines if a 'None' option should be added to the picker.
        /// </summary>
        private bool ShouldAddNoneOption(InteractivePickerType? pickerType, List<StepOption> stepOptions)
        {
            return pickerType is not InteractivePickerType.YesNo && // Don't add it for Yes/No
                   !_parameter.Required && // Only add it if its not required
                   stepOptions.Any(x => x.Name.Equals("None")); // Don't add it if its already in the list
        }

        private static List<StepOption> GetDynamicValues(IFlowContext context, Parameter parameter)
        {
            List<string> values = [];

            // dynamically calculate the parameter values for Entra ID for performance reasons
            if (string.Equals(parameter.DisplayName, AspnetStrings.Options.Username.DisplayName, StringComparison.Ordinal))
            {
                values = AnsiConsole
                    .Status()
                    .WithSpinner()
                    .Start("Retrieving Azure CLI usernames...", statusContext =>
                    {
                        return AzCliHelper.GetUsernameParameterValuesDynamically(context);
                    });
            }
            else if (string.Equals(parameter.DisplayName, AspnetStrings.Options.TenantId.DisplayName, StringComparison.Ordinal))
            {
                values = AnsiConsole
                    .Status()
                    .WithSpinner()
                    .Start("Retrieving Azure CLI tenant IDs...", statusContext =>
                    {
                        return AzCliHelper.GetTenantParameterValuesDynamically(context);
                    });
            }
            else if (string.Equals(parameter.DisplayName, AspnetStrings.Options.SelectApplication.DisplayName, StringComparison.Ordinal))
            {
                values = AnsiConsole
                    .Status()
                    .WithSpinner()
                    .Start("Retrieving Azure CLI app IDs...", statusContext =>
                    {
                        return AzCliHelper.GetAppIdParameterValuesDynamically(context);
                    });
            }
            return [.. values.Select(x => new StepOption() { Name = x, Value = x })];
        }

        /// <summary>
        /// Gets custom picker values as step options.
        /// </summary>
        private static List<StepOption> GetCustomValues(IEnumerable<string>? customPickerValues)
        {
            if (customPickerValues is null || customPickerValues.Count() == 0)
            {
                throw new InvalidOperationException("Missing 'Parameter.CustomPickerValues' values!.\nNeeded when using 'Parameter.InteractivePicker.CustomPicker'");
            }

            return customPickerValues.Select(x => new StepOption() { Name = x, Value = x }).ToList();
        }

        /// <summary>
        /// Gets the display name for a step option.
        /// </summary>
        private static string GetDisplayName(StepOption stepOption)
        {
            bool displayNone = stepOption.Name.Equals("None", StringComparison.OrdinalIgnoreCase);
            if (displayNone)
            {
                return $"[sandybrown]{stepOption.Name} (empty to skip parameter)[/]";
            }

            return string.Equals(stepOption.Name, stepOption.Value, StringComparison.Ordinal)
                ? Markup.Escape(stepOption.Name)
                : $"{stepOption.Name} {stepOption.Value.ToSuggestion(withBrackets: true)}";
        }

        /// <summary>
        /// Gets the display name for a project step option.
        /// </summary>
        private string GetDisplayNameForProjects(StepOption stepOption)
        {
            var pathDisplay = stepOption.Value.MakeRelativePath(_environmentService.CurrentDirectory) ?? stepOption.Value;
            return $"{stepOption.Name} {pathDisplay.ToSuggestion(withBrackets: true)}";
        }

        /// <summary>
        /// Gets the display name for a yes/no step option.
        /// </summary>
        private string GetDisplayNameForYesNo(StepOption stepOption) => stepOption.Name;

        /// <summary>
        /// Validates the user input for the parameter.
        /// </summary>
        private ValidationResult Validate(IFlowContext context, string promptVal)
        {
            if (!_parameter.Required && string.IsNullOrEmpty(promptVal))
            {
                return ValidationResult.Success();
            }

            if (!string.IsNullOrEmpty(promptVal) && !ParameterHelpers.CheckType(_parameter.Type, promptVal))
            {
                return ValidationResult.Error("Invalid input, please try again.");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Gets all class display names from the code service.
        /// </summary>
        internal static async Task<List<StepOption>> GetClassDisplayNamesAsync(ICodeService codeService, CommandInfo? command)
        {
            var isViewsCommand = string.Equals(command?.Name, AspnetStrings.RazorView.Views, StringComparison.OrdinalIgnoreCase);
            var allClassSymbols = isViewsCommand
                ? await codeService.GetAllClassSymbolsAsync(includeReferencedProjects: true)
                : await codeService.GetAllClassSymbolsAsync();

            List<StepOption> classNames = [];
            if (allClassSymbols != null && allClassSymbols.Count != 0)
            {
                allClassSymbols.ForEach(
                x =>
                {
                    if (x != null)
                    {
                        var name = isViewsCommand ? x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : x.MetadataName;
                        var value = isViewsCommand ? name : x.Name;
                        classNames.Add(new StepOption() { Name = name, Value = value });
                    }
                });
            }

            return classNames;
        }

        /// <summary>
        /// Gets document names as step options from a list of Roslyn documents.
        /// </summary>
        internal static List<StepOption> GetDocumentNames(List<Document> documents)
        {
            List<StepOption> classNames = [];
            if (documents != null && documents.Count != 0)
            {
                documents.ForEach(
                x =>
                {
                    if (x != null)
                    {
                        string fileName = System.IO.Path.GetFileName(x.Name);
                        classNames.Add(new StepOption() { Name = fileName, Value = x.Name });
                    }
                });
            }

            return classNames;
        }

        /// <summary>
        /// Gets project files as step options from the file system.
        /// </summary>
        internal List<StepOption> GetProjectFiles()
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

            List<string> projects = _fileSystem.EnumerateFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories).ToList();
            var slnFiles = _fileSystem.EnumerateFiles(workingDirectory, "*.sln", SearchOption.TopDirectoryOnly).ToList();
            List<string> projectsFromSlnFiles = GetProjectsFromSolutionFiles(slnFiles, workingDirectory);
            projects = AddUniqueProjects(projects, projectsFromSlnFiles);

            if (projects.Count == 0)
            {
                projects = _fileSystem.EnumerateFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories).ToList();
            }

            return projects.Select(x => new StepOption() { Name = GetProjectDisplayName(x), Value = x }).ToList();
        }

        /// <summary>
        /// Gets project paths from a list of solution files.
        /// </summary>
        internal List<string> GetProjectsFromSolutionFiles(List<string> solutionFiles, string workingDir)
        {
            List<string> projectPaths = [];
            foreach (var solutionFile in solutionFiles)
            {
                projectPaths.AddRange(GetProjectsFromSolutionFile(solutionFile, workingDir));
            }

            return projectPaths;
        }

        /// <summary>
        /// Adds unique project paths to the base list.
        /// </summary>
        internal List<string> AddUniqueProjects(List<string> baseList, List<string> projectPathsToAdd)
        {
            var baseProjectNames = baseList.Select(x => Path.GetFileName(x));
            foreach (var projectPath in projectPathsToAdd)
            {
                var normalizedPath = StringUtil.NormalizePathSeparators(projectPath);
                var projectName = Path.GetFileName(normalizedPath);
                if (!baseProjectNames.Contains(projectName, StringComparer.OrdinalIgnoreCase))
                {
                    baseList.Add(normalizedPath);
                }
            }

            return baseList;
        }

        /// <summary>
        /// Gets project paths from a single solution file.
        /// </summary>
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

        /// <summary>
        /// Gets the display name for a project path.
        /// </summary>
        private static string GetProjectDisplayName(string projectPath)
        {
            return Path.GetFileNameWithoutExtension(projectPath);
        }

        private static List<StepOption>? _dbProviders;
        private static List<StepOption> DbProviders
        {
            get
            {
                _dbProviders ??=
                [
                    new() { Name = "SQL Server", Value = "sqlserver" },
                    new() { Name = "SQLite", Value = "sqlite" },
                    new() { Name = "PostgreSQL", Value = "postgres" },
                    new() { Name = "Cosmos DB", Value = "cosmos" }
                ];

                return _dbProviders;
            }
        }

        /// <summary>
        /// Represents an option for a step in the parameter discovery flow.
        /// </summary>
        internal class StepOption
        {
            public required string Name { get; set; }
            public required string Value { get; set; }
        }
    }
}
