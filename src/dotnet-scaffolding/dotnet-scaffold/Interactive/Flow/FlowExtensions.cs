// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Command;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow
{
    /// <summary>
    /// Extension methods for <see cref="IFlowContext"/> to simplify retrieval of common scaffolding context values.
    /// These helpers provide strongly-typed accessors for frequently used context properties.
    /// </summary>
    internal static class FlowContextExtensions
    {
        /// <summary>
        /// Gets the telemetry environment variables from the context.
        /// </summary>
        public static IDictionary<string, string>? GetTelemetryEnvironmentVariables(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IDictionary<string, string>>(FlowContextProperties.TelemetryEnvironmentVariables, throwIfEmpty);
        }
        /// <summary>
        /// Gets the code service from the context.
        /// </summary>
        public static ICodeService? GetCodeService(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<ICodeService>(FlowContextProperties.CodeService, throwIfEmpty);
        }

        /// <summary>
        /// Gets the selected component object from the context.
        /// </summary>
        public static DotNetToolInfo? GetComponentObj(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<DotNetToolInfo>(FlowContextProperties.ComponentObj, throwIfEmpty);
        }

        /// <summary>
        /// Gets the selected command object from the context.
        /// </summary>
        public static CommandInfo? GetCommandObj(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<CommandInfo>(FlowContextProperties.CommandObj, throwIfEmpty);
        }

        /// <summary>
        /// Gets the list of command argument values from the context.
        /// </summary>
        public static List<string>? GetCommandArgValues(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<List<string>>(FlowContextProperties.CommandArgValues, throwIfEmpty);
        }

        /// <summary>
        /// Gets the list of command infos from the context.
        /// </summary>
        public static IList<KeyValuePair<string, CommandInfo>>? GetCommandInfos(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IList<KeyValuePair<string, CommandInfo>>>(FlowContextProperties.CommandInfos, throwIfEmpty);
        }

        /// <summary>
        /// Gets the list of scaffolding categories from the context.
        /// </summary>
        public static List<string>? GetScaffoldingCategories(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<List<string>?>(FlowContextProperties.ScaffoldingCategories, throwIfEmpty);
        }

        /// <summary>
        /// Gets the chosen scaffolding category from the context.
        /// </summary>
        public static string? GetChosenCategory(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<string>(FlowContextProperties.ChosenCategory, throwIfEmpty);
        }

        /// <summary>
        /// Gets the source project path from the context.
        /// </summary>
        public static string? GetSourceProjectPath(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<string>(FlowContextProperties.SourceProjectPath, throwIfEmpty);
        }

        /// <summary>
        /// Gets the command settings from the context.
        /// </summary>
        public static ScaffoldCommand.Settings? GetCommandSettings(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<ScaffoldCommand.Settings>(FlowContextProperties.CommandSettings, throwIfEmpty);
        }

        /// <summary>
        /// Gets the remaining arguments from the context.
        /// </summary>
        public static IRemainingArguments? GetRemainingArgs(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IRemainingArguments>(FlowContextProperties.RemainingArgs, throwIfEmpty);
        }

        /// <summary>
        /// Gets the command arguments dictionary from the context.
        /// </summary>
        public static IDictionary<string, List<string>>? GetArgsDict(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<IDictionary<string, List<string>>>(FlowContextProperties.CommandArgs, throwIfEmpty);
        }
        /// <summary>
        /// Configures a Spectre.Console status spinner with a highlight style.
        /// </summary>
        public static Status WithSpinner(this Status status)
        {
            return status
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Styles.Highlight);
        }

        /// <summary>
        /// Helper to get a value from the context or throw if not found and required.
        /// </summary>
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
