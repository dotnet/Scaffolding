// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow
{
    internal static class FlowContextExtensions
    {
        public static string? GetComponentName(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<string>(FlowContextProperties.ComponentName, throwIfEmpty);
        }

        public static string? GetSourceProjectPath(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<string>(FlowContextProperties.SourceProjectPath, throwIfEmpty);
        }

        public static ScaffoldCommand.Settings? GetCommandSettings(this IFlowContext context, bool throwIfEmpty = false)
        {
            return context.GetValueOrThrow<ScaffoldCommand.Settings>(FlowContextProperties.CommandSettings, throwIfEmpty);
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
