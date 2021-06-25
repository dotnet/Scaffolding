// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Project;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.Extensions.Internal;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    public static class CodeWriter
    {
        internal static void WriteConfiguration(Summary summary, IEnumerable<Replacement> replacements, ApplicationParameters reconciledApplicationParameters, IConsoleLogger consoleLogger)
        {
            foreach (var replacementsInFile in replacements.GroupBy(r => r.FilePath))
            {
                string filePath = replacementsInFile.Key;

                string fileContent = File.ReadAllText(filePath);
                bool updated = false;
                foreach (Replacement r in replacementsInFile.OrderByDescending(r => r.Index))
                {
                    string? replaceBy = ComputeReplacement(r.ReplaceBy, reconciledApplicationParameters, consoleLogger);
                    if (replaceBy != null && replaceBy!=r.ReplaceFrom)
                    {
                        int index = fileContent.IndexOf(r.ReplaceFrom /*, r.Index*/);
                        if (index != -1)
                        {
                            fileContent = fileContent.Substring(0, index)
                                + replaceBy
                                + fileContent.Substring(index + r.Length);
                            updated = true;
                            summary.changes.Add(new Change($"{filePath}: updating {r.ReplaceBy}"));
                        }
                    }
                }

                if (updated)
                {
                    // Keep a copy of the original
                    if (!File.Exists(filePath + "%"))
                    {
                        File.Copy(filePath, filePath + "%");
                    }
                    File.WriteAllText(filePath, fileContent);
                }
            }
        }

        //TODO : Add integration tests for testing instead of mocking for unit tests.
        public static void AddUserSecrets(bool isB2C, string projectPath, string value, IConsoleLogger consoleLogger)
        {
            //init regardless. If it's already initiated, dotnet-user-secrets confirms it.
            InitUserSecrets(projectPath, consoleLogger);
            string section = isB2C ? "AzureADB2C" : "AzureAD";
            string key = $"{section}:ClientSecret";
            SetUserSecerets(projectPath, key, value, consoleLogger);
        }

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
            consoleLogger.LogMessage("Initializing User Secrets . . . ", LogMessageType.Error);

            var result = Command.CreateDotNet(
                "user-secrets",
                arguments.ToArray())
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();
            
            if (result.ExitCode != 0)
            {
                consoleLogger.LogMessage("FAILED\n\n", LogMessageType.Error, removeNewLine: true);
                throw new Exception("Error while running dotnet-user-secrets init");
            }
            else
            {
                consoleLogger.LogMessage("SUCCESS\n\n", removeNewLine: true);
            }
        }

        public static void AddPackage(string packageName, string tfm, IConsoleLogger consoleLogger, string? packageVersion = null)
        {
            if (!string.IsNullOrEmpty(packageName) && ((!string.IsNullOrEmpty(packageVersion)) || (!string.IsNullOrEmpty(tfm))))
            {
                var errors = new List<string>();
                var output = new List<string>();
                var arguments = new List<string>();
                arguments.Add("package");
                arguments.Add(packageName);
                if (!string.IsNullOrEmpty(packageVersion))
                {
                    arguments.Add("-v");
                    arguments.Add(packageVersion);
                }

                if (IsTfmPreRelease(tfm))
                {
                    arguments.Add("--prerelease");
                }

                if (!string.IsNullOrEmpty(tfm))
                {
                    arguments.Add("-f");
                    arguments.Add(tfm);
                }

                consoleLogger.LogMessage($"Adding package {packageName} . . . ");

                var result = Command.CreateDotNet(
                    "add",
                    arguments.ToArray())
                    .OnErrorLine(e => errors.Add(e))
                    .OnOutputLine(o => output.Add(o))
                    .Execute();

                if (result.ExitCode != 0)
                {
                    consoleLogger.LogMessage("FAILED\n\n", removeNewLine: true);
                    consoleLogger.LogMessage($"Failed to add package {packageName}");
                }
                else
                {
                    consoleLogger.LogMessage("SUCCESS\n\n");
                }
            }
        }

        private static bool IsTfmPreRelease(string tfm)
        {
            return tfm.Equals("net6.0", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetUserSecerets(string projectPath, string key, string value, IConsoleLogger consoleLogger)
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
                consoleLogger.LogMessage($"\nAdded {key} to user secrets.\n");
            }
        }

        private static string? ComputeReplacement(string replaceBy, ApplicationParameters reconciledApplicationParameters, IConsoleLogger consoleLogger)
        {
            string? replacement = replaceBy;
            switch(replaceBy)
            {
                case "Application.ClientSecret":
                    string? password = reconciledApplicationParameters.PasswordCredentials.LastOrDefault();
                    if (!string.IsNullOrEmpty(reconciledApplicationParameters.SecretsId) && !string.IsNullOrEmpty(password))
                    {
                        AddUserSecrets(reconciledApplicationParameters.IsB2C, reconciledApplicationParameters.ProjectPath ?? string.Empty, password, consoleLogger);
                    }
                    else
                    {
                        replacement = password;
                    }
                    break;
                case "Application.ClientId":
                    replacement = reconciledApplicationParameters.ClientId;
                    break;
                case "Directory.TenantId":
                    replacement = reconciledApplicationParameters.TenantId;
                    break;
                case "Directory.Domain":
                    replacement = reconciledApplicationParameters.Domain;
                    break;
                case "Application.SusiPolicy":
                    replacement = reconciledApplicationParameters.SusiPolicy;
                    break;
                case "Application.CallbackPath":
                    replacement = reconciledApplicationParameters.CallbackPath;
                    break;
                case "profilesApplicationUrls":
                case "iisSslPort":
                case "iisApplicationUrl":
                    replacement = null;
                    break;
                case "secretsId":
                    replacement = reconciledApplicationParameters.SecretsId;
                    break;
                case "targetFramework":
                    replacement = reconciledApplicationParameters.TargetFramework;
                    break;
                case "Application.Authority":
                    replacement = reconciledApplicationParameters.Authority;
                    // Blazor b2C
                    replacement = replacement?.Replace("onmicrosoft.com.b2clogin.com", "b2clogin.com");

                    break;
                case "MsalAuthenticationOptions":
                    // Todo generalize with a directive: Ensure line after line, or ensure line
                    // between line and line
                    replacement = reconciledApplicationParameters.MsalAuthenticationOptions;
                    if (reconciledApplicationParameters.AppIdUri == null)
                    {
                        replacement +=
                            "\n                options.ProviderOptions.DefaultAccessTokenScopes.Add(\"User.Read\");";

                    }                    
                    break;
                case "Application.CalledApiScopes":
                    replacement = reconciledApplicationParameters.CalledApiScopes
                        ?.Replace("openid", string.Empty)
                        ?.Replace("offline_access", string.Empty)
                        ?.Trim();
                    break;

                case "Application.Instance":
                    if (reconciledApplicationParameters.Instance == "https://login.microsoftonline.com/tfp/"
                        && reconciledApplicationParameters.IsB2C
                        && !string.IsNullOrEmpty(reconciledApplicationParameters.Domain)
                        && reconciledApplicationParameters.Domain.EndsWith(".onmicrosoft.com"))
                    {
                        replacement = "https://"+reconciledApplicationParameters.Domain.Replace(".onmicrosoft.com", ".b2clogin.com")
                            .Replace("aadB2CInstance", reconciledApplicationParameters.Domain1);
                    }
                    else
                    {
                        replacement = reconciledApplicationParameters.Instance;
                    }
                    break;
                case "Application.ConfigurationSection":
                    replacement = null;
                    break;
                case "Application.AppIdUri":
                    replacement = reconciledApplicationParameters.AppIdUri;
                    break;

                default:
                    Console.WriteLine($"{replaceBy} not known");
                    break;
            }
            return replacement;
        }
    }
}
