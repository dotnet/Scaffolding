// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.DotNet.MsIdentity.AuthenticationParameters;
using Microsoft.DotNet.MsIdentity.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.MsIdentity.CodeReaderWriter
{
    public class CodeWriter
    {
        internal void WriteConfiguration(Summary summary, IEnumerable<Replacement> replacements, ApplicationParameters reconcialedApplicationParameters)
        {
            foreach (var replacementsInFile in replacements.GroupBy(r => r.FilePath))
            {
                string filePath = replacementsInFile.Key;

                string fileContent = File.ReadAllText(filePath);
                bool updated = false;
                foreach (Replacement r in replacementsInFile.OrderByDescending(r => r.Index))
                {
                    string? replaceBy = ComputeReplacement(r.ReplaceBy, reconcialedApplicationParameters);
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

        private string? ComputeReplacement(string replaceBy, ApplicationParameters reconciledApplicationParameters)
        {
            string? replacement = replaceBy;
            switch(replaceBy)
            {
                case "Application.ClientSecret":
                    string? password = reconciledApplicationParameters.PasswordCredentials.LastOrDefault();
                    if (!string.IsNullOrEmpty(reconciledApplicationParameters.SecretsId))
                    {
                        // TODO: adapt for Linux: https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-5.0&tabs=windows#how-the-secret-manager-tool-works
                        string? envVariable = Environment.GetEnvironmentVariable("UserProfile");
                        if (!string.IsNullOrEmpty(envVariable))
                        {
                            string path = Path.Combine(
                                envVariable,
                                @"AppData\Roaming\Microsoft\UserSecrets\",
                                reconciledApplicationParameters.SecretsId,
                                "secrets.json")!;
                            if (!File.Exists(path))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                                string section = reconciledApplicationParameters.IsB2C ? "AzureADB2C" : "AzureAD";
                                File.WriteAllText(path, $"{{\n    \"{section}:ClientSecret\": \"{password}\"\n}}");
                                replacement = "See user secrets";
                            }
                            else
                            {
                                replacement = password;
                            }
                        }
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
