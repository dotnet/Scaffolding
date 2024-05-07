// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ParameterDiscovery
    {
        private readonly Parameter _parameter;
        public ParameterDiscovery(Parameter parameter)
        {
            _parameter = parameter;
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
                var codeService = context.GetCodeService();
                if (codeService != null)
                {
                    return await PromptInteractivePicker(context, _parameter.PickerType, codeService) ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private async Task<string?> PromptInteractivePicker(IFlowContext context, InteractivePickerType? pickerType, ICodeService codeService)
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
                        return (await codeService.GetAllClassSymbolsAsync()).ToList();
                    });

                    displayTuples = GetClassDisplayNames(allClassSymbols);
                    interactiveTitle = "Class";
                    break;
                case InteractivePickerType.FilePicker:
                    var allDocuments = (await codeService.GetAllDocumentsAsync()).ToList();
                    displayTuples = GetDocumentNames(allDocuments);
                    interactiveTitle = "File";
                    break;
                case InteractivePickerType.DbProviderPicker:
                    displayTuples = DbProviders;
                    interactiveTitle = "DbProvider";
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

        private IList<Tuple<string, string>> GetClassDisplayNames(List<ISymbol> compilationClassSymbols)
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
