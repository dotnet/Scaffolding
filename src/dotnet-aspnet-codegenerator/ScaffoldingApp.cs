// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    internal class ScaffoldingApp: CommandLineApplication
    {
        private const string APPNAME = "aspnet-codegenerator";
        private static readonly string paramDefinitionFilePath = Path.Combine("Generators", "ParameterDefinitions");
        public ScaffoldingApp(bool throwOnUnexpectedArg)
            : base(throwOnUnexpectedArg)
        {
            Name = APPNAME;
            Description = Resources.AppDesc;
            GeneratorArgument = this.Argument("generator", Resources.GeneratorArgumentDesc);
            ProjectPath = this.Option("-p|--project", Resources.ProjectPathOptionDesc, CommandOptionType.SingleValue);
            PackagesPath = this.Option("-n|--nuget-package-dir", "", CommandOptionType.SingleValue);
            AppConfiguration =this.Option("-c|--configuration", Resources.ConfigurationOptionDesc, CommandOptionType.SingleValue);
            Framework = this.Option("-tfm|--target-framework", Resources.TargetFrameworkOptionDesc, CommandOptionType.SingleValue);
            BuildBasePath = this.Option("-b|--build-base-path", "", CommandOptionType.SingleValue);
            NoBuild = this.Option("--no-build", "", CommandOptionType.NoValue);
        }

        public IProjectContext ProjectContext { get; set; }

        public CommandOption ProjectPath { get; }
        public CommandOption PackagesPath { get; }
        public CommandOption AppConfiguration { get; }
        public CommandOption Framework { get; }
        public CommandOption BuildBasePath { get; }
        public CommandOption NoBuild { get; }

        public CommandArgument GeneratorArgument { get; }


        public override string GetHelpText(string commandName=null)
        {
            string helpText = base.GetHelpText(commandName);
            StringBuilder sb = new StringBuilder(helpText);

            // Search for NuGetPackages with CodeGenerationPackages.
            if (ProjectContext != null)
            {
                var nuGetPackages = ProjectContext.PackageDependencies.Select(d => d.Path);
                var paramDefinitionsCache = BuildParamDefinitionCache(nuGetPackages);

                if (!string.IsNullOrEmpty(GeneratorArgument.Value))
                {
                    sb.AppendLine();
                    sb.AppendLine(string.Format(Resources.SelectedCodeGeneratorStr, GeneratorArgument.Value));
                    ParamDefinition generatorParamDef = null;
                    if (paramDefinitionsCache.TryGetValue(GeneratorArgument.Value, out generatorParamDef))
                    {
                        sb.AppendLine(BuildHelpForGenerator(generatorParamDef));
                    }
                    else
                    {
                        // Generator not available.
                        sb.AppendLine(string.Format(Resources.NoCodeGeneratorFound, GeneratorArgument.Value));
                        sb.AppendLine(GetHelpTextForAvailableGenerators(paramDefinitionsCache));
                    }
                }
                else
                {
                    sb.AppendLine(GetHelpTextForAvailableGenerators(paramDefinitionsCache));
                }

            }
            else
            {
                Debug.Print("Project Context was not set. Cannot search for generators to display help.");
            }

            return sb.ToString();
        }

        private static string GetHelpTextForAvailableGenerators(Dictionary<string, ParamDefinition> paramDefinitionsCache)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();

            if (!paramDefinitionsCache.Any())
            {
                // No parameterDefinitions were found in any of the nuget packages.
                // This can happen if the project doesn't have a reference to 'Microsoft.VisualStudio.Web.CodeGeneration.Design' package.
                sb.Append(Resources.NoCodeGeneratorsFound);
                sb.Append(Resources.AddDesignPackage);
                return sb.ToString();
            }

            sb.AppendLine(Resources.AvailableGeneratorsHeader);
            var maxLenGeneratorName = paramDefinitionsCache.Max(p => p.Key.Length);
            var formatStr = "  {0,-"+maxLenGeneratorName+"}: {1}";
            foreach(var paramDef in paramDefinitionsCache.Values)
            {
                sb.AppendLine(string.Format(formatStr, paramDef.Alias, paramDef.Description));
            }

            return sb.ToString();
        }

        internal void ShowHelpWithoutProjectInformation()
        {

        }

        private static string BuildHelpForGenerator( ParamDefinition generatorParamDef)
        {
            StringBuilder sb = new StringBuilder();
            if (generatorParamDef.Arguments != null && generatorParamDef.Arguments.Any())
            {
                sb.AppendLine();
                sb.AppendLine(Resources.GeneratorArgsHeader);
                var maxLenArgument = generatorParamDef.Arguments.Max(a => a.Name.Length);
                var formatStr = "  {0,-"+maxLenArgument+"} : {1}";
                foreach (var arg in generatorParamDef.Arguments)
                {
                    sb.AppendLine(string.Format(formatStr, arg.Name, arg.Description));
                }
            }

            if (generatorParamDef.Options != null && generatorParamDef.Options.Any())
            {
                sb.AppendLine();
                sb.AppendLine(Resources.GeneratorOptionsHeader);
                var maxLenOption = generatorParamDef.Options.Max(o => o.Name.Length + o.ShortName.Length) + 4;
                var formatStr = "  {0,-"+maxLenOption+"} : {1}";
                
                foreach (var opt in generatorParamDef.Options)
                {
                    if (string.IsNullOrEmpty(opt.ShortName))
                    {
                        sb.AppendLine(string.Format(formatStr, "--"+opt.Name, opt.Description));
                    }
                    else
                    {
                        sb.AppendLine(string.Format(formatStr, "--"+opt.Name+"|-"+opt.ShortName, opt.Description));
                    }
                }
            }

            return sb.ToString();
        }

        private static Dictionary<string, ParamDefinition> BuildParamDefinitionCache(IEnumerable<string> nuGetPackages)
        {
            Dictionary<string, ParamDefinition> paramDefinitionsCache = new Dictionary<string, ParamDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in nuGetPackages)
            {
                var generatorParamDir = Path.Combine(path, paramDefinitionFilePath);
                if (Directory.Exists(generatorParamDir))
                {
                    try
                    {
                        var paramDefinitionFiles = Directory.EnumerateFiles(generatorParamDir, "*.json", SearchOption.TopDirectoryOnly);
                        if (paramDefinitionFiles == null)
                        {
                            continue;
                        }
                        foreach (var paramFile in paramDefinitionFiles)
                        {
                            var json = File.ReadAllText(paramFile);
                            var paramDefinition = JsonConvert.DeserializeObject<ParamDefinition>(json);
                            paramDefinitionsCache[paramDefinition.Alias] = paramDefinition;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Do not want to fail if there is a problem with the json file.
                        Debug.Fail(ex.Message);
                        continue;
                    }
                }
            }
            return paramDefinitionsCache;
        }
    }

}