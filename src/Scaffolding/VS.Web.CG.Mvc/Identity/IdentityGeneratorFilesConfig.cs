// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public static class IdentityGeneratorFilesConfig
    {
        public static string[] GetAreaFolders(bool isDataRequired)
        {
            return isDataRequired
                ? new string[]
                {
                    "Data",
                    "Pages"
                }
                : new string[]
                {
                    "Pages"
                };
        }

        internal static IdentityGeneratorFile IdentityHostingStartup = new IdentityGeneratorFile()
        {
            Name = "IdentityHostingStartup",
            SourcePath = "IdentityHostingStartup.cshtml",
            OutputPath = "Areas/Identity/IdentityHostingStartup.cs",
            IsTemplate = true,
            ShouldOverWrite = OverWriteCondition.Never,
            ShowInListFiles = false
        };

        private static IdentityGeneratorFile ReadMe = new IdentityGeneratorFile()
        {
            Name = "ScaffoldingReadme",
            SourcePath = "ScaffoldingReadme.cshtml",
            OutputPath = $"./{Constants.ReadMeOutputFileName}",
            IsTemplate = false,
            ShouldOverWrite = OverWriteCondition.Always,
            ShowInListFiles = false
        };

        private static IdentityGeneratorFile ViewStart = new IdentityGeneratorFile()
        {
            Name = "_ViewStart",
            SourcePath = "_ViewStart.cshtml",
            OutputPath = "Areas/Identity/Pages/_ViewStart.cshtml",
            IsTemplate = true,
            ShowInListFiles = false,
            ShouldOverWrite = OverWriteCondition.Never
        };

        private static IdentityGeneratorFile[] ViewImports = new[]
        {
            // Order is important here.
            new IdentityGeneratorFile()
            {
                Name = "Account.Manage._ViewImports",
                SourcePath = "Account.Manage._ViewImports.cshtml",
                OutputPath = "Areas/Identity/Pages/Account/Manage/_ViewImports.cshtml",
                IsTemplate = true,
                ShouldOverWrite = OverWriteCondition.Never
            },
            new IdentityGeneratorFile()
            {
                Name = "Account._ViewImports",
                SourcePath = "Account._ViewImports.cshtml",
                OutputPath = "Areas/Identity/Pages/Account/_ViewImports.cshtml",
                IsTemplate = true,
                ShouldOverWrite = OverWriteCondition.Never
            },
            new IdentityGeneratorFile()
            {
                Name= "_ViewImports",
                SourcePath= "_ViewImports.cshtml",
                OutputPath= "Areas/Identity/Pages/_ViewImports.cshtml",
                IsTemplate= true,
                ShouldOverWrite = OverWriteCondition.Never
            }
        };

        internal static IdentityGeneratorFile[] GetFilesToGenerate(IEnumerable<string> names, IdentityGeneratorTemplateModel templateModel)
        {
            if (templateModel == null)
            {
                throw new ArgumentNullException(nameof(templateModel));
            }

            List<IdentityGeneratorFile> filesToGenerate = GetDataModelFiles(templateModel);

            if (templateModel.GenerateLayout)
            {
                IdentityGeneratorFile layout = new IdentityGeneratorFile()
                {
                    Name = "_Layout",
                    SourcePath = "_Layout.cshtml",
                    OutputPath = Path.Combine(templateModel.SupportFileLocation, "_Layout.cshtml"),
                    IsTemplate = true,
                    ShowInListFiles = false
                };
                filesToGenerate.Add(layout);
            }
            else
            {
                IdentityGeneratorFile validationScriptsPartial = new IdentityGeneratorFile()
                {
                    Name = "_ValidationScriptsPartial",
                    SourcePath = "Pages/_ValidationScriptsPartial.cshtml",
                    OutputPath = "Areas/Identity/Pages/_ValidationScriptsPartial.cshtml",
                    IsTemplate = false,
                    ShouldOverWrite = OverWriteCondition.Never
                };
                filesToGenerate.Add(validationScriptsPartial);
            }

            if (!string.IsNullOrEmpty(templateModel.Layout))
            {
                filesToGenerate.Add(ViewStart);

                // if there's a layout file, generate a _ValidationScriptsPartial.cshtml in the same place.
                IdentityGeneratorFile layoutPeerValidationScriptsPartial = new IdentityGeneratorFile()
                {
                    Name = "_ValidationScriptsPartial",
                    SourcePath = "Pages/_ValidationScriptsPartial.cshtml",
                    OutputPath = Path.Combine(templateModel.SupportFileLocation, "_ValidationScriptsPartial.cshtml"),
                    IsTemplate = false,
                    ShouldOverWrite = OverWriteCondition.Never
                };
                filesToGenerate.Add(layoutPeerValidationScriptsPartial);
            }

            string contentVersion;
            if (templateModel is IdentityGeneratorTemplateModel2 templateModel2)
            {
                contentVersion = templateModel2.ContentVersion;
            }
            else
            {
                contentVersion = IdentityGenerator.ContentVersionDefault;
            }

            IdentityGeneratorFiles config = GetConfigContentVersion(contentVersion);

            if (!templateModel.UseDefaultUI)
            {
                if (names != null && names.Any())
                {
                    foreach (var name in names)
                    {
                        filesToGenerate.AddRange(config.NamedFileConfig[name]);
                    }
                }
                else
                {
                    filesToGenerate.AddRange(config.NamedFileConfig
                                                    .Where(x => !string.Equals(x.Key, "wwwroot", StringComparison.OrdinalIgnoreCase))
                                                    .SelectMany(f => f.Value));
                }
            }

            if (!templateModel.HasExistingNonEmptyWwwRoot)
            {
                filesToGenerate.AddRange(config.NamedFileConfig["WwwRoot"]);
            }
            filesToGenerate.Add(IdentityHostingStartup);


            filesToGenerate.Add(ReadMe);

            return filesToGenerate.Distinct(new IdentityGeneratorFileComparer()).ToArray();
        }

        /// <summary>
        /// Returns a list of ViewImport template files based on the files to generate and also on which _ViewImports already exist in the project.
        /// We start from the innermost directory requested, if a _ViewImports file exists at this level, we are done.
        /// If not, we add one to be generated and then look in the outer directory.
        /// Sequence is: 
        ///    - Account.Manage._ViewImports
        ///    - Account._ViewImports
        ///    - _ViewImports
        /// </summary>
        internal static List<IdentityGeneratorFile> GetViewImports(IEnumerable<IdentityGeneratorFile> files, IFileSystem fileSystem, string rootPath)
        {
            if (files == null || !files.Any())
            {
                return new List<IdentityGeneratorFile>();
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentException(nameof(rootPath));
            }

            var requiredViewImports = new List<IdentityGeneratorFile>();

            foreach (var viewImport in ViewImports)
            {
                var fileNamePrefix = viewImport.Name.Substring(0, viewImport.Name.Length - _ViewImportFileName.Length);
                if (files.Any(f => f.Name.StartsWith(fileNamePrefix)))
                {
                    if (fileSystem.FileExists(Path.Combine(rootPath, viewImport.OutputPath)))
                    {
                        break;
                    }

                    requiredViewImports.Add(viewImport);
                }
            }

            return requiredViewImports;
        }

        private static readonly string _ViewImportFileName = "_ViewImports";

        // Note: "peer" is somewhat of a misnomer, not all of the "peer" files are in the same directory as the layout file.
        internal static bool TryGetLayoutPeerFiles(IFileSystem fileSystem, string rootPath, IdentityGeneratorTemplateModel templateModel, out IReadOnlyList<IdentityGeneratorFile> layoutPeerFiles, bool isBlazorProject)
        {
            string viewImportsFileNameWithExtension = string.Concat(_ViewImportFileName, ".cshtml");

            if (fileSystem.FileExists(Path.Combine(rootPath, templateModel.SupportFileLocation, viewImportsFileNameWithExtension)))
            {
                layoutPeerFiles = null;
                return false;
            }

            const string sharedDirName = "Shared";
            string outputDirectory;

            string checkSupportFileLocation = templateModel.SupportFileLocation;
            if (checkSupportFileLocation.EndsWith("\\") || checkSupportFileLocation.EndsWith("/"))
            {
                checkSupportFileLocation = checkSupportFileLocation.Substring(0, checkSupportFileLocation.Length - 1);
            }

            if (checkSupportFileLocation.EndsWith(sharedDirName))
            {
                int directoryLengthWithoutShared = checkSupportFileLocation.Length - sharedDirName.Length;
                outputDirectory = checkSupportFileLocation.Substring(0, directoryLengthWithoutShared);
            }
            else
            {
                outputDirectory = templateModel.SupportFileLocation;
            }

            List<IdentityGeneratorFile> peerFiles = new List<IdentityGeneratorFile>();

            IdentityGeneratorFile layoutPeerViewImportsFile = new IdentityGeneratorFile()
            {
                Name = "_ViewImports",
                SourcePath = "SupportPages._ViewImports.cshtml",
                OutputPath = Path.Combine(outputDirectory, viewImportsFileNameWithExtension),
                IsTemplate = true,
                ShouldOverWrite = OverWriteCondition.Never
            };
            peerFiles.Add(layoutPeerViewImportsFile);

            IdentityGeneratorFile layoutPeerViewStart = new IdentityGeneratorFile()
            {
                Name = "_ViewStart",
                SourcePath = "SupportPages._ViewStart.cshtml",
                OutputPath = Path.Combine(outputDirectory, "_ViewStart.cshtml"),
                IsTemplate = true,
                ShowInListFiles = false,
                ShouldOverWrite = OverWriteCondition.Never
            };

            //don't need Layout for the start page of a Blazor Server application.
            //Still adding Layout file for other added pages.
            if (!isBlazorProject)
            {
                peerFiles.Add(layoutPeerViewStart);
            }

            layoutPeerFiles = peerFiles;
            return true;
        }

        private static readonly string _CookieConsentPartialFileName = "_CookieConsentPartial.cshtml";

        // Look for a cookie consent file in the same location as the layout file. If there isn't one, setup the config to add one there.
        internal static bool TryGetCookieConsentPartialFile(IFileSystem fileSystem, string rootPath, IdentityGeneratorTemplateModel templateModel, out IdentityGeneratorFile cookieConsentPartialConfig)
        {
            string layoutDir = Path.GetDirectoryName(templateModel.Layout);

            string cookieConsentCheckLocation = Path.Combine(layoutDir, _CookieConsentPartialFileName);
            if (fileSystem.FileExists(cookieConsentCheckLocation))
            {
                cookieConsentPartialConfig = null;
                return false;
            }

            cookieConsentPartialConfig = new IdentityGeneratorFile()
            {
                Name = "_CookieConsentPartial",
                SourcePath = "SupportPages._CookieConsentPartial.cshtml",
                OutputPath = Path.Combine(layoutDir, _CookieConsentPartialFileName),
                IsTemplate = false,
                ShowInListFiles = false,
                ShouldOverWrite = OverWriteCondition.Never
            };
            return true;
        }

        private static List<IdentityGeneratorFile> GetDataModelFiles(IdentityGeneratorTemplateModel templateModel)
        {
            var filesToGenerate = new List<IdentityGeneratorFile>();
            if (!templateModel.IsUsingExistingDbContext)
            {
                // Add DbContext template.
                filesToGenerate.Add(new IdentityGeneratorFile()
                {
                    IsTemplate = true,
                    Name = "ApplicationDbContext",
                    SourcePath = "ApplicationDbContext.cshtml",
                    OutputPath = Path.Combine("Areas", "Identity", "Data", $"{templateModel.DbContextClass}.cs"),
                    ShowInListFiles = false
                });

                if (templateModel.IsGenerateCustomUser)
                {
                    // Add custom user class template.
                    filesToGenerate.Add(new IdentityGeneratorFile()
                    {
                        IsTemplate = true,
                        Name = "ApplicationUser",
                        SourcePath = "ApplicationUser.cshtml",
                        OutputPath = Path.Combine("Areas", "Identity", "Data", $"{templateModel.UserClass}.cs"),
                        ShowInListFiles = false
                    });
                }
            }

            // add the _LoginPartial file. Its config will cause it to not be created if it already exists.
            filesToGenerate.Add(new IdentityGeneratorFile()
            {
                IsTemplate = true,
                Name = "_LoginPartial",
                SourcePath = "_LoginPartial.cshtml",
                OutputPath = Path.Combine(templateModel.SupportFileLocation, "_LoginPartial.cshtml"),
                ShouldOverWrite = OverWriteCondition.Never,
                ShowInListFiles = false
            });

            return filesToGenerate;
        }

        // Maps the valid values for ContentVersion to the filename prefix for the appropriate {prefix}_identitygeneratorfilesconfig.json
        private static readonly IReadOnlyDictionary<string, string> _contentVersionToConfigPrefixMap = new Dictionary<string, string>()
        {
            { IdentityGenerator.ContentVersionBootstrap3, "bootstrap3" },
            { IdentityGenerator.ContentVersionBootstrap4, "bootstrap4" },
            { IdentityGenerator.ContentVersionDefault, "bootstrap5" },
        };

        // Lazy-Caches the deserialized versions of {prefix}_identitygeneratorfilesconfig.json
        private static Dictionary<string, IdentityGeneratorFiles> _versionedConfigCache = new Dictionary<string, IdentityGeneratorFiles>();

        // Returns the list of files for the specified content version.
        public static IEnumerable<string> GetFilesToList(string contentVersion)
        {
            IdentityGeneratorFiles config = GetConfigContentVersion(contentVersion);

            return config.NamedFileConfig
                .Where(c => c.Value.Any(f => f.ShowInListFiles))
                .Select(c => c.Key);
        }

        // Returns the list of files for the default content version.
        public static IEnumerable<string> GetFilesToList()
        {
            return GetFilesToList(IdentityGenerator.ContentVersionDefault);
        }

        private static IdentityGeneratorFiles GetConfigContentVersion(string contentVersion)
        {
            IdentityGeneratorFiles config;

            if (!_versionedConfigCache.TryGetValue(contentVersion, out config))
            {
                string configString = string.Empty;

                if (_contentVersionToConfigPrefixMap.TryGetValue(contentVersion, out string configFileNamePrefix))
                {
                    string configFileName = $"Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity.{configFileNamePrefix}_identitygeneratorfilesconfig.json";
                    using (var configStream = typeof(IdentityGeneratorFilesConfig).Assembly.GetManifestResourceStream(configFileName))
                    using (var reader = new StreamReader(configStream))
                    {
                        configString = reader.ReadToEnd();
                    }
                }

                config = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentityGeneratorFiles>(configString);

                _versionedConfigCache[contentVersion] = config;
            }

            return config;
        }
    }
}
