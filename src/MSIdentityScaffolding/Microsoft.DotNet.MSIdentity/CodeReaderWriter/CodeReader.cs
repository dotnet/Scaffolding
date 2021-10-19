// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml;
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
            string folderToConfigure,
            ProjectDescription projectDescription,
            IEnumerable<ProjectDescription> projectDescriptions)
        {
            ProjectAuthenticationSettings projectAuthenticationSettings = new ProjectAuthenticationSettings(projectDescription);
            ProcessProject(
                folderToConfigure,
                projectDescription,
                projectAuthenticationSettings,
                projectDescriptions);
            return projectAuthenticationSettings;
        }

        private static void ProcessProject(
            string folderToConfigure,
            ProjectDescription projectDescription,
            ProjectAuthenticationSettings projectAuthenticationSettings,
            IEnumerable<ProjectDescription> projectDescriptions)
        {
            string projectPath = Path.Combine(folderToConfigure, projectDescription.ProjectRelativeFolder!);

            // TO-DO get all the project descriptions
            var properties = projectDescription.GetMergedConfigurationProperties(projectDescriptions).ToArray();
            foreach (ConfigurationProperties configurationProperties in properties)
            {
                string? filePath = Directory.EnumerateFiles(projectPath, configurationProperties.FileRelativePath!).FirstOrDefault();
                ProcessFile(projectAuthenticationSettings, filePath, configurationProperties);
            }

            foreach (var matchesForProjectType in projectDescription.GetMergedMatchesForProjectType(projectDescriptions))
            {
                if (!string.IsNullOrEmpty(matchesForProjectType.Sets))
                {
                    projectAuthenticationSettings.ApplicationParameters.Sets(matchesForProjectType.Sets);
                }
            }

            PostProcessWebUris(projectAuthenticationSettings.ApplicationParameters, projectAuthenticationSettings.Replacements); // Why bother doing this from VS?
        }

        private static void PostProcessWebUris(ApplicationParameters applicationParameters, List<Replacement> replacements)
        {
            Debugger.Break(); // TODO z we can do the redirect URIs here perhaps
            // TODO: ProvisioningToolOptions.ProjectType == "blazorwasm"; 
            bool isBlazorWasm = applicationParameters.IsBlazorWasm == true && applicationParameters.IsWebApp != true;

            string callbackPath = isBlazorWasm ? "/authentication/login-callback" : applicationParameters.CallbackPath ?? "/signin-oidc";

            if (!callbackPath.StartsWith('/'))
            {
                applicationParameters.WebRedirectUris.Add(callbackPath);
            }
            else
            {
                List<string> launchUrls = new List<string>();

                // Create a list of string (roots)
                string? iisExpressApplicationUrl = replacements.FirstOrDefault(r => r.ReplaceBy == "iisApplicationUrl")?.ReplaceFrom;
                string? iisExpressSslPort = replacements.FirstOrDefault(r => r.ReplaceBy == "iisSslPort")?.ReplaceFrom;
                if (!string.IsNullOrEmpty(iisExpressApplicationUrl) && !string.IsNullOrEmpty(iisExpressSslPort))
                {
                    // Change the port
                    Uri url = new Uri(iisExpressApplicationUrl);
                    string sslLauchUrl;

                    if (url.Scheme == Uri.UriSchemeHttps || url.Port == 0)
                    {
                        sslLauchUrl = iisExpressApplicationUrl;
                    }
                    else if (int.TryParse(iisExpressSslPort, out var port))
                    {
                        sslLauchUrl = new UriBuilder
                        {
                            Scheme = Uri.UriSchemeHttps,
                            Host = url.Host,
                            Port = port,
                            Path = url.AbsolutePath,
                            Query = url.Query
                        }.Uri.AbsoluteUri;

                        if (!iisExpressApplicationUrl.EndsWith('/'))
                        {
                            sslLauchUrl = sslLauchUrl.TrimEnd('/');
                        }
                        launchUrls.Add(sslLauchUrl);
                    }
                }

                // Add the profile launchsettings 
                IEnumerable<string> httpsProfileLaunchUrls = replacements
                    .Where(r => r.ReplaceBy == "profilesApplicationUrls")
                    .SelectMany(r => r.ReplaceFrom.Split(';'))
                    .Where(u => u.StartsWith(Uri.UriSchemeHttps));
                launchUrls.AddRange(httpsProfileLaunchUrls);

                // Set the web redirect URIs
                List<string> redirectUris = applicationParameters.WebRedirectUris;
                redirectUris.AddRange(launchUrls.Select(l => l + callbackPath));

                // Get the signout path (/oidc-signout)
                string signoutPath = isBlazorWasm ? "/authentication/logout-callback" : "/oidc-signout";

                if (launchUrls.Any())
                {
                    applicationParameters.LogoutUrl = launchUrls.First() + signoutPath;
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
                        Uri authority = new Uri(value);
                        string? tenantOrDomain = authority.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                        if (tenantOrDomain == "qualified.domain.name")
                        {
                            tenantOrDomain = null;
                        }
                        projectAuthenticationSettings.ApplicationParameters.Domain = tenantOrDomain;
                        projectAuthenticationSettings.ApplicationParameters.TenantId = tenantOrDomain;
                    }
                    break;
                case "Directory.Domain":
                    projectAuthenticationSettings.ApplicationParameters.Domain = value;
                    projectAuthenticationSettings.ApplicationParameters.EffectiveDomain = (value != defaultValue) ? value : null;
                    break;
                case "secretsId":
                    Debugger.Break();
                    Debug.WriteLine(value);
                    //projectAuthenticationSettings.ApplicationParameters.SecretsId = value; // TODO do we need this?
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
