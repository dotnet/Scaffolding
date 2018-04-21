// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                    "Pages",
                    "Services"
                }
                : new string[]
                {
                    "Pages",
                    "Services"
                };
        }

        private static IdentityGeneratorFile IdentityHostingStartup = new IdentityGeneratorFile()
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
            OutputPath = "./ScaffoldingReadme.txt",
            IsTemplate = true,
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
                    IsTemplate = false
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

            if (!templateModel.UseDefaultUI)
            {
                if (names != null && names.Any())
                {
                    foreach (var name in names)
                    {
                        filesToGenerate.AddRange(_config.NamedFileConfig[name]);
                    }
                }
                else
                {
                    filesToGenerate.AddRange(_config.NamedFileConfig.SelectMany(f => f.Value));
                }
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

        internal static bool TryGetLayoutPeerFiles(IFileSystem fileSystem, string rootPath, IdentityGeneratorTemplateModel templateModel, out IReadOnlyList<IdentityGeneratorFile> layoutPeerFiles)
        {
            string viewImportsFileNameWithExtension = string.Concat(_ViewImportFileName, ".cshtml");

            if (fileSystem.FileExists(Path.Combine(rootPath, templateModel.SupportFileLocation, viewImportsFileNameWithExtension)))
            {
                layoutPeerFiles = null;
                return false;
            }

            const string sharedDirName = "Shared/";
            string outputDirectory;
            if (templateModel.SupportFileLocation.EndsWith(sharedDirName))
            {
                int directoryLengthWithoutShared = templateModel.SupportFileLocation.Length - sharedDirName.Length;
                outputDirectory = templateModel.SupportFileLocation.Substring(0, directoryLengthWithoutShared);
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
            peerFiles.Add(layoutPeerViewStart);

            layoutPeerFiles = peerFiles;
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

        private static IdentityGeneratorFiles _config;

        static IdentityGeneratorFilesConfig()
        {
            string configStr = string.Empty;
            const string configFileName = "Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity.identitygeneratorfilesconfig.json";
            using (var configStream = typeof(IdentityGeneratorFilesConfig).Assembly.GetManifestResourceStream(configFileName))
            using (var reader = new StreamReader(configStream))
            {
                configStr = reader.ReadToEnd();
            }

            _config = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentityGeneratorFiles>(configStr);
        }

        public static IEnumerable<string> GetFilesToList()
        {
            return _config.NamedFileConfig
                .Where(c => c.Value.Any(f => f.ShowInListFiles))
                .Select(c => c.Key);
        }
    }
}