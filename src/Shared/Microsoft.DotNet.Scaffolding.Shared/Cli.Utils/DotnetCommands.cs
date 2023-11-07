// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.Internal;
using NuGet.Versioning;

namespace Microsoft.DotNet.Scaffolding.Shared.Cli.Utils
{
    internal static class DotnetCommands
    {
        /// <summary>
        /// Use to execute `dotnet new`. Mostly for scaffolders that can be invoked using dotnet new
        /// </summary>
        /// <param name="projectPath">csproj path for the project being scaffolded.</param>
        /// <param name="additionalArgs">additional arguments </param>
        /// <param name="consoleLogger">IConsoleLogger for console output</param>
        public static void ExecuteDotnetNew(string projectPath, IList<string> additionalArgs, ILogger consoleLogger)
        {
            //need IList<string> populated with at least the template name.
            if (additionalArgs == null)
            {
                throw new ArgumentNullException(nameof(additionalArgs));
            }

            string templateName = additionalArgs.FirstOrDefault();
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException(nameof(additionalArgs));
            }

            var errors = new List<string>();
            var output = new List<string>();

            List<string> arguments = new List<string>();
            //if project path is present, use it for dotnet new
            if (!string.IsNullOrEmpty(projectPath))
            {
                arguments.Add("--project");
                arguments.Add(projectPath);
            }

            arguments.AddRange(additionalArgs);
            string argumentsString = string.Join(" ", arguments);
            consoleLogger.LogMessage($"\nExecuting 'dotnet new {argumentsString}'", LogMessageLevel.Information);
            //check for minimum dotnet version
            string dotnetVersion = GetDotnetCommandVersion(consoleLogger);
            bool validDotnetVersion = true;

            if (SemanticVersion.TryParse(dotnetVersion, out var parsedVersion))
            {
                validDotnetVersion = parsedVersion.CompareTo(MinimumDotnetVersion) >= 0;
                if (!validDotnetVersion)
                {
                    consoleLogger.LogMessage($"\nFound dotnet version ({parsedVersion}). {MessageStrings.DotnetRequirementNotMet}");
                }
            }
            else
            {
                consoleLogger.LogMessage("Could not find the dotnet version, running the `dotnet new` command anyways.\n");
            }

            if (validDotnetVersion)
            {
                var result = Command.CreateDotNet(
                "new",
                arguments.ToArray())
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

                if (result.ExitCode != 0)
                {
                    consoleLogger.LogMessage(MessageStrings.FailedDotnetNew, LogMessageLevel.Error);

                    if (errors != null)
                    {
                        string errorMessage = string.Empty;
                        errorMessage += $"{Environment.NewLine} {string.Join(Environment.NewLine, errors)} ";
                        throw new Exception(errorMessage);
                    }
                }
                else
                {
                    consoleLogger.LogMessage($"{MessageStrings.Success}\n");
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="consoleLogger"></param>
        /// <exception cref="Exception"></exception>
        public static void InitUserSecrets(string projectPath, IConsoleLogger consoleLogger)
        {
            var errors = new List<string>();
            var output = new List<string>();

            IList<string> arguments = new List<string>();

            //if project path is present, use it for dotnet user-secrets
            if (!string.IsNullOrEmpty(projectPath))
            {
                arguments.Add("-p");
                arguments.Add(projectPath);
            }

            arguments.Add("init");
            consoleLogger.LogMessage(MessageStrings.InitializeUserSecrets, LogMessageType.Error);

            var result = Command.CreateDotNet(
                "user-secrets",
                arguments.ToArray())
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode != 0)
            {
                consoleLogger.LogMessage(MessageStrings.Failed, LogMessageType.Error, removeNewLine: true);
                throw new Exception(MessageStrings.DotnetUserSecretsError);
            }
            else
            {
                consoleLogger.LogMessage($"{MessageStrings.Success}\n\n", removeNewLine: true);
            }
        }

        public static void SetUserSecrets(string projectPath, string key, string value, IConsoleLogger consoleLogger)
        {
            var errors = new List<string>();
            var output = new List<string>();

            IList<string> arguments = new List<string>();

            //if project path is present, use it for dotnet user-secrets
            if (!string.IsNullOrEmpty(projectPath))
            {
                arguments.Add("-p");
                arguments.Add(projectPath);
            }

            arguments.Add("set");
            arguments.Add(key);
            arguments.Add(value);
            var result = Command.CreateDotNet(
                "user-secrets",
                arguments)
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error while running dotnet-user-secrets set {key} {value}");
            }
            else
            {
                string consoleOutput = string.Format(MessageStrings.AddingKeyToUserSecrets, key);
                consoleLogger.LogMessage($"\n{consoleOutput}\n");
            }
        }

        public static void AddPackage(string packageName, string tfm, IConsoleLogger consoleLogger, string packageVersion = null)
        {
            if (!string.IsNullOrEmpty(packageName) && ((!string.IsNullOrEmpty(packageVersion)) || (!string.IsNullOrEmpty(tfm))))
            {
                var errors = new List<string>();
                var output = new List<string>();
                var arguments = new List<string>
                {
                    "package",
                    packageName
                };

                if (!string.IsNullOrEmpty(packageVersion))
                {
                    arguments.Add("-v");
                    arguments.Add(packageVersion);
                }

                if (ProjectModelHelper.IsTfmPreRelease(tfm))
                {
                    arguments.Add("--prerelease");
                }

                if (!string.IsNullOrEmpty(tfm))
                {
                    arguments.Add("-f");
                    arguments.Add(tfm);
                }

                consoleLogger.LogMessage(string.Format(MessageStrings.AddingPackage, packageName));

                var result = Command.CreateDotNet(
                    "add",
                    arguments.ToArray())
                    .OnErrorLine(e => errors.Add(e))
                    .OnOutputLine(o => output.Add(o))
                    .Execute();

                if (result.ExitCode != 0)
                {
                    consoleLogger.LogMessage($"{MessageStrings.Failed}\n\n", removeNewLine: true);
                    consoleLogger.LogMessage(string.Format(MessageStrings.FailedAddPackage, packageName));
                }
                else
                {
                    consoleLogger.LogMessage($"{MessageStrings.Success}\n\n");
                }
            }
        }

        public static string GetDotnetCommandVersion(ILogger consoleLogger)
        {
            string dotnetVersion = "";
            var errors = new List<string>();
            var output = new List<string>();
            var arguments = new List<string>
            {
                "--version"
            };

            var result = Command.CreateDotNet(
                string.Empty,
                arguments.ToArray())
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode == 0)
            {
                dotnetVersion = output.FirstOrDefault();
            }

            return dotnetVersion;
        }

        private static readonly SemanticVersion MinimumDotnetVersion = SemanticVersion.Parse("8.0.100-preview.2.23153.6");
    }
}
