// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

/// <summary>
/// Provides extension methods for configuring console logging with the <see cref="CleanConsoleFormatter"/>.
/// </summary>
internal static class ConsoleLoggerExtensions
{
    /// <summary>
    /// Adds the <see cref="CleanConsoleFormatter"/> to the logging builder, configuring it as the default console formatter.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <returns>The same <see cref="ILoggingBuilder"/> instance for chaining.</returns>
    public static ILoggingBuilder AddCleanConsoleFormatter(this ILoggingBuilder builder)
        => builder.AddConsole(options => options.FormatterName = nameof(CleanConsoleFormatter))
                  .AddConsoleFormatter<CleanConsoleFormatter, CleanConsoleFormatterOptions>();
}
