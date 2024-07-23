// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

internal static class ConsoleLoggerExtensions
{
    public static ILoggingBuilder AddCleanConsoleFormatter(this ILoggingBuilder builder)
        => builder.AddConsole(options => options.FormatterName = nameof(CleanConsoleFormatter))
                  .AddConsoleFormatter<CleanConsoleFormatter, CleanConsoleFormatterOptions>();
}
