// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml;
using Microsoft.DotNet.MSIdentity.Project;
using ConfigurationProperties = Microsoft.DotNet.MSIdentity.Project.ConfigurationProperties;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeReader
    {
        static readonly JsonSerializerOptions s_serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderToConfigure"></param>
        /// <param name="projectDescription"></param>
        /// <param name="projectDescriptions"></param>
        /// <returns></returns>
        public ProjectAuthenticationSettings ReadFromFiles(
            ProjectDescription projectDescription,
            IEnumerable<ProjectDescription> projectDescriptions,
            IEnumerable<string> files)
        {
            ProjectAuthenticationSettings projectAuthenticationSettings = new ProjectAuthenticationSettings(projectDescription);
            ProcessProject(
                projectDescription,
                projectAuthenticationSettings,
                projectDescriptions,
                files);
            return projectAuthenticationSettings;
        }

        private void ProcessProject(
            ProjectDescription projectDescription,
            ProjectAuthenticationSettings projectAuthenticationSettings,
            IEnumerable<ProjectDescription> projectDescriptions,
            IEnumerable<string> files)
        {
            // TO-DO get all the project descriptions
            var properties = projectDescription.GetMergedConfigurationProperties(projectDescriptions);
            foreach (ConfigurationProperties configurationProperties in properties)
            {
                string? filePath = files.Where(f => f.Contains(configurationProperties.FileRelativePath!)).FirstOrDefault();
                ProcessFile(projectAuthenticationSettings, filePath, configurationProperties);
            }

            foreach (var matchesForProjectType in projectDescription.GetMergedMatchesForProjectType(projectDescriptions))
            {
                if (!string.IsNullOrEmpty(matchesForProjectType.Sets))
                {
                    projectAuthenticationSettings.ApplicationParameters.Sets(matchesForProjectType.Sets);
                }
            }

            PostProcessWebUris(projectAuthenticationSettings);
        }

        private void PostProcessWebUris(ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            bool isBlazorWasm = projectAuthenticationSettings.ApplicationParameters.IsBlazorWasm;
            string callbackPath = projectAuthenticationSettings.ApplicationParameters.CallbackPath ?? "/signin-oidc";
            if (isBlazorWasm)
            {
                callbackPath = "/authentication/login-callback";
            }
            if (callbackPath != null && !callbackPath.StartsWith('/'))
            {
                projectAuthenticationSettings.ApplicationParameters.WebRedirectUris.Add(callbackPath);
            }
            else
            {
                List<string> launchUrls = new List<string>();

                // Create a list of string (roots)
                string? iisExpressApplicationUrl = projectAuthenticationSettings.Replacements.FirstOrDefault(r => r.ReplaceBy == "iisApplicationUrl")?.ReplaceFrom;
                string? iisExpressSslPort = projectAuthenticationSettings.Replacements.FirstOrDefault(r => r.ReplaceBy == "iisSslPort")?.ReplaceFrom;
                if (!string.IsNullOrEmpty(iisExpressApplicationUrl) && !string.IsNullOrEmpty(iisExpressSslPort))
                {
                    // Change the port
                    Uri url = new Uri(iisExpressApplicationUrl);
                    string sslLauchUrl;

                    if (url.Scheme == "https" || url.Port == 0)
                    {
                        sslLauchUrl = iisExpressApplicationUrl;
                    }
                    else
                    {
                        sslLauchUrl = "https://" + url.Host + ":" + iisExpressSslPort + url.PathAndQuery;
                        if (!iisExpressApplicationUrl.EndsWith('/'))
                        {
                            sslLauchUrl = sslLauchUrl.TrimEnd('/');
                        }
                    }
                    launchUrls.Add(sslLauchUrl);
                }

                // Add the profile lauchsettings 
                IEnumerable<string> httpsProfileLaunchUrls = projectAuthenticationSettings.Replacements
                    .Where(r => r.ReplaceBy == "profilesApplicationUrls")
                    .SelectMany(r => r.ReplaceFrom.Split(';'))
                    .Where(u => u.StartsWith("https://"));
                launchUrls.AddRange(httpsProfileLaunchUrls);

                // Set the web redirect URIs
                List<string> redirectUris = projectAuthenticationSettings.ApplicationParameters.WebRedirectUris;
                redirectUris.AddRange(launchUrls.Select(l => l + callbackPath));

                // Get the signout path (/oidc-signout)
                string signoutPath = "/oidc-signout";
                if (isBlazorWasm)
                {
                    signoutPath = "/authentication/logout-callback";
                }
                if (!string.IsNullOrEmpty(signoutPath))
                {
                    if (signoutPath.StartsWith("/"))
                    {
                        if (launchUrls.Any())
                        {
                            projectAuthenticationSettings.ApplicationParameters.LogoutUrl = launchUrls.First() + signoutPath;
                        }
                    }
                    else
                    {
                        projectAuthenticationSettings.ApplicationParameters.LogoutUrl = signoutPath;
                    }
                }
            }
        }

        private static void ProcessFile(
            ProjectAuthenticationSettings projectAuthenticationSettings,
            string? filePath,
            ConfigurationProperties file)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                JsonElement jsonContent = default;
                XmlDocument? xmlDocument = null;

                if (filePath.EndsWith(".json"))
                {
                    jsonContent = JsonSerializer.Deserialize<JsonElement>(fileContent,
                        s_serializerOptionsWithComments);
                }
                else if (filePath.EndsWith(".csproj"))
                {
                    xmlDocument = new XmlDocument();
                    xmlDocument.Load(filePath);
                }

                foreach (PropertyMapping propertyMapping in file.Properties)
                {
                    bool found = false;
                    string? property = propertyMapping.Property;
                    if (property != null)
                    {
                        string[] path = property.Split(':');

                        if (filePath.EndsWith(".json"))
                        {
                            IEnumerable<KeyValuePair<JsonElement, int>> elements = FindMatchingElements(jsonContent, path, 0);
                            foreach (var pair in elements)
                            {
                                JsonElement element = pair.Key;
                                int index = pair.Value;
                                found = true;
                                string replaceFrom = element.ValueKind == JsonValueKind.Number
                                    ? element.GetInt32().ToString(CultureInfo.InvariantCulture)
                                    : element.ToString()!;

                                UpdatePropertyRepresents(
                                    projectAuthenticationSettings,
                                    filePath,
                                    propertyMapping,
                                    index,
                                    replaceFrom);
                            }
                        }

                        else if (xmlDocument != null)
                        {
                            XmlNode? node = FindMatchingElement(xmlDocument, path);
                            if (node != null)
                            {
                                UpdatePropertyRepresents(
                                    projectAuthenticationSettings,
                                    filePath,
                                    propertyMapping,
                                    0,
                                    node.InnerText);
                            }
                        }
                        else
                        {
                            int index = fileContent.IndexOf(property);
                            if (index != -1)
                            {
                                UpdatePropertyRepresents(
                                    projectAuthenticationSettings,
                                    filePath,
                                    propertyMapping,
                                    0,
                                    property);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(propertyMapping.Sets) && (found
                            || (propertyMapping.MatchAny != null && propertyMapping.MatchAny.Any(m => fileContent.Contains(m)))))
                    {
                        projectAuthenticationSettings.ApplicationParameters.Sets(propertyMapping.Sets);
                    }
                }
                // TODO: else AddNotFound?
            }
        }

        private static void UpdatePropertyRepresents(
            ProjectAuthenticationSettings projectAuthenticationSettings,
            string filePath,
            PropertyMapping propertyMapping,
            int index,
            string replaceFrom)
        {
            if (!string.IsNullOrEmpty(propertyMapping.Represents))
            {
                ReadCodeSetting(
                    propertyMapping.Represents,
                    replaceFrom,
                    propertyMapping.Default,
                    projectAuthenticationSettings);
                int length = replaceFrom.Length;

                AddReplacement(
                    projectAuthenticationSettings,
                    filePath,
                    index,
                    length,
                    replaceFrom,
                    propertyMapping.Represents);
            }
        }

        /// <summary>
        /// Recursively finds the Json elements matching the path
        /// </summary>
        /// <param name="parentElement">parent JsonElement</param>
        /// <param name="path">Json path to match. Note that "*" can be used to match anything
        /// (for example path representing "profiles:*:applicationUrl" in an appsettings.json to 
        /// get all the redirect URIs)</param>
        /// <param name="offset"></param>
        /// <returns>An enumeration of JSonElement matching the path</returns>
        private static IEnumerable<KeyValuePair<JsonElement, int>> FindMatchingElements(
            JsonElement parentElement,
            IEnumerable<string> path,
            int offset)
        {
            if (path.Any())
            {
                string segment = path.First();

                IEnumerable<JsonProperty> props = parentElement.EnumerateObject()
                    .Where(e => segment == "*" || e.Name == segment);
                foreach (JsonProperty prop in props)
                {
                    if (prop.Value.ValueKind != JsonValueKind.Undefined)
                    {
                        JsonElement element = prop.Value;
                        int index = GetIndex(element) + offset;

                        if (path.Count() == 1)
                        {
                            yield return new KeyValuePair<JsonElement, int>(element, index);
                        }
                        foreach (KeyValuePair<JsonElement, int> child in FindMatchingElements(element, path.Skip(1), index))
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        private static XmlNode? FindMatchingElement(XmlDocument parentElement, IEnumerable<string> path)
        {
#pragma warning disable S1075 // URIs should not be hardcoded
            string xPath = "/" + string.Join("/", path);
#pragma warning restore S1075 // URIs should not be hardcoded
            XmlNode? node = parentElement.SelectSingleNode(xPath);
            return node;
        }

        private static void ReadCodeSetting(
            string represents,
            string value,
            string? defaultValue,
            ProjectAuthenticationSettings projectAuthenticationSettings)
        {
            switch (represents)
            {
                case "Application.ClientId":
                    projectAuthenticationSettings.ApplicationParameters.ClientId = value;
                    projectAuthenticationSettings.ApplicationParameters.EffectiveClientId = (value != defaultValue) ? value : null;
                    break;
                case "Application.CallbackPath":
                    projectAuthenticationSettings.ApplicationParameters.CallbackPath = value ?? defaultValue;
                    break;
                case "Directory.TenantId":
                    projectAuthenticationSettings.ApplicationParameters.TenantId = value;
                    projectAuthenticationSettings.ApplicationParameters.EffectiveTenantId = (value != defaultValue) ? value : null;
                    break;
                case "Application.Authority":
                    // Case of Blazorwasm where the authority is not separated :(
                    projectAuthenticationSettings.ApplicationParameters.Authority = value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        // TODO: something more generic
                        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? authority))
                        {
                            var tenantOrDomain = authority.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                            if (tenantOrDomain != "qualified.domain.name")
                            {
                                projectAuthenticationSettings.ApplicationParameters.Domain = tenantOrDomain;
                                projectAuthenticationSettings.ApplicationParameters.TenantId = tenantOrDomain;
                            }
                        }
                    }
                    break;
                case "Directory.Domain":
                    projectAuthenticationSettings.ApplicationParameters.Domain = value;
                    projectAuthenticationSettings.ApplicationParameters.EffectiveDomain = (value != defaultValue) ? value : null;
                    break;
                case "secretsId":
                    projectAuthenticationSettings.ApplicationParameters.SecretsId = value;
                    break;
                case "targetFramework":
                    projectAuthenticationSettings.ApplicationParameters.TargetFramework = value;
                    break;
                case "MsalAuthenticationOptions":
                    projectAuthenticationSettings.ApplicationParameters.MsalAuthenticationOptions = value;
                    break;
                case "Application.CalledApiScopes":
                    projectAuthenticationSettings.ApplicationParameters.CalledApiScopes = value;
                    break;
                case "Application.Instance":
                    projectAuthenticationSettings.ApplicationParameters.Instance = value;
                    break;
                case "Application.SusiPolicy":
                    projectAuthenticationSettings.ApplicationParameters.SusiPolicy = value;
                    break;
                case "Application.AppIdUri":
                    projectAuthenticationSettings.ApplicationParameters.AppIdUri = value;
                    break;
            }
        }

        private static int GetIndex(JsonElement element)
        {
            Type type = element.GetType()!;
            object _idx = type.GetField("_idx",
                                        BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(element)!;
            return (int)_idx;
        }

        private static void AddReplacement(
            ProjectAuthenticationSettings projectAuthenticationSettings,
            string filePath,
            int index,
            int length,
            string replaceFrom,
            string replaceBy)
        {
            projectAuthenticationSettings.Replacements.Add(new Replacement(filePath, index, length, replaceFrom, replaceBy));
        }

    }
}
