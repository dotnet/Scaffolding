// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow
{
    internal static class FlowContextExtensions
    {
        public static ICodeService? GetCodeService(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<ICodeService>(FlowContextProperties.CodeService, throwIfEmpty);
        }

        public static DotNetToolInfo? GetComponentObj(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<DotNetToolInfo>(FlowContextProperties.ComponentObj, throwIfEmpty);
        }

        public static CommandInfo? GetCommandObj(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<CommandInfo>(FlowContextProperties.CommandObj, throwIfEmpty);
        }

        public static List<string>? GetCommandArgValues(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<List<string>>(FlowContextProperties.CommandArgValues, throwIfEmpty);
        }

        public static IList<KeyValuePair<string, CommandInfo>>? GetCommandInfos(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IList<KeyValuePair<string, CommandInfo>>>(FlowContextProperties.CommandInfos, throwIfEmpty);
        }

        public static string? GetScaffoldingCategory(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<string>(FlowContextProperties.ScaffoldingCategory, throwIfEmpty);
        }

        public static string? GetSourceProjectPath(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<string>(FlowContextProperties.SourceProjectPath, throwIfEmpty);
        }

        public static ScaffoldCommand.Settings? GetCommandSettings(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<ScaffoldCommand.Settings>(FlowContextProperties.CommandSettings, throwIfEmpty);
        }

        public static IRemainingArguments? GetRemainingArgs(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IRemainingArguments>(FlowContextProperties.RemainingArgs, throwIfEmpty);
        }

        public static IDictionary<string, List<string>>? GetArgsDict(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IDictionary<string, List<string>>>(FlowContextProperties.CommandArgs, throwIfEmpty);
        }
        public static Status WithSpinner(this Status status)
        {
            return status
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Aesthetic)
                .SpinnerStyle(Styles.Highlight);
        }

        private static T? GetValueOrThrow<T>(this IFlowContext context, string propertyName, bool throwIfEmpty = false)
        {
            var value = context.GetValue<T>(propertyName);
            if (throwIfEmpty && value is null)
            {
                throw new ArgumentNullException(propertyName);
            }

            return value;
        }
    }

}
