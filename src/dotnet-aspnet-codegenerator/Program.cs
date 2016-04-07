// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CodeGeneration.EntityFrameworkCore;
using Microsoft.Extensions.CodeGeneration.Templating;
using Microsoft.Extensions.CodeGeneration.Templating.Compilation;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Frameworks;

namespace Microsoft.Extensions.CodeGeneration
{
    public class Program
    {
        private static ConsoleLogger _logger;
        private const string APPNAME = "Code Generation";
        private const string APP_DESC = "Code generation for Asp.net Core";
        private static bool invokeProjectDependencyCommand = false;
        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            _logger = new ConsoleLogger();
            var app = new CommandLineApplication(false)
            {
                Name = APPNAME,
                Description = APP_DESC
            };
            app.HelpOption("-h|--help");
            var projectPath = app.Option("-p|--project", "Path to project.json", CommandOptionType.SingleValue);
            var packagesPath = app.Option("-n|--nugetPackageDir", "Path to check for Nuget packages", CommandOptionType.SingleValue);
            var appConfiguration = app.Option("-c|--configuration", "Configuration for the project (Possible values: Debug/ Release)", CommandOptionType.SingleValue);
            
            var context = CreateProjectContext(projectPath.Value());
            if(invokeProjectDependencyCommand) 
            {
                    
            }
            app.OnExecute(() =>
            {
                var serviceProvider = new ServiceProvider();
                

                var configuration = appConfiguration.Value() ?? "Debug";
                if(configuration != null && !configuration.Equals("Release") && !configuration.Equals("Debug")) 
                {
                    throw new ArgumentException($"Invalid value for configuration: {configuration}. {appConfiguration.Description}");
                }
                Directory.SetCurrentDirectory(context.ProjectDirectory);
                AddFrameworkServices(serviceProvider, context, packagesPath.Value());
                AddCodeGenerationServices(serviceProvider);
                var codeGenCommand = serviceProvider.GetService<CodeGenCommand>();
                codeGenCommand.Execute(app.RemainingArguments.ToArray());
                return 0;
            });
            
            app.Execute(args);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            _logger.LogMessage("RunTime " + elapsedTime, LogMessageLevel.Information);
        }

        private static void AddFrameworkServices(ServiceProvider serviceProvider, ProjectContext context, string nugetPackageDir)
        {
            var applicationEnvironment = new ApplicationInfo(context.RootProject.Identity.Name, context.ProjectDirectory);
            serviceProvider.Add(typeof(IServiceProvider), serviceProvider);
            serviceProvider.Add(typeof(ProjectContext), context);
            serviceProvider.Add(typeof(Workspace), context.CreateWorkspace());
            serviceProvider.Add(typeof(IApplicationInfo), applicationEnvironment);
            serviceProvider.Add(typeof(ICodeGenAssemblyLoadContext), DefaultAssemblyLoadContext.CreateAssemblyLoadContext(nugetPackageDir));
            serviceProvider.Add(typeof(ILibraryManager), new LibraryManager(context));
            serviceProvider.Add(typeof(ILibraryExporter), new LibraryExporter(context, applicationEnvironment));
        }

        private static ProjectContext CreateProjectContext(string projectPath)
        {
            projectPath = Path.GetFullPath(projectPath ?? Directory.GetCurrentDirectory());

            if (!projectPath.EndsWith(Microsoft.DotNet.ProjectModel.Project.FileName))
            {
                projectPath = Path.Combine(projectPath, Microsoft.DotNet.ProjectModel.Project.FileName);
            }

            if (!File.Exists(projectPath))
            {
                throw new InvalidOperationException($"{projectPath} does not exist.");
            }
            var contexts = ProjectContext.CreateContextForEachFramework(projectPath);
            var frameworks = contexts.Select(c => c.TargetFramework);
            var nearestNugetFramework = NuGetFrameworkUtility.GetNearest(frameworks, FrameworkConstants.CommonFrameworks.NetStandardApp15, f => new NuGetFramework(f));
            if(nearestNugetFramework == null) 
            {
                nearestNugetFramework = NuGetFrameworkUtility.GetNearest(frameworks, FrameworkConstants.CommonFrameworks.Net451, f => new NuGetFramework(f));
                invokeProjectDependencyCommand = true;
            }
            
            if(nearestNugetFramework != null) 
            {
                return contexts.Where(c => c.TargetFramework == nearestNugetFramework).First();
            }
            throw new Exception("The project does not target any frameworks compatible with 'netstandard1.5'");
        }

        private static void AddCodeGenerationServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            //Ordering of services is important here
            serviceProvider.Add(typeof(ILogger), _logger);
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<ICodeGeneratorAssemblyProvider, DefaultCodeGeneratorAssemblyProvider>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorLocator, CodeGeneratorsLocator>();
            serviceProvider.AddServiceWithDependencies<CodeGenCommand, CodeGenCommand>();

            serviceProvider.AddServiceWithDependencies<ICompilationService, RoslynCompilationService>();
            serviceProvider.AddServiceWithDependencies<ITemplating, RazorTemplating>();

            serviceProvider.AddServiceWithDependencies<IPackageInstaller, PackageInstaller>();

            serviceProvider.AddServiceWithDependencies<IModelTypesLocator, ModelTypesLocator>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorActionsService, CodeGeneratorActionsService>();
            
            serviceProvider.AddServiceWithDependencies<IDbContextEditorServices, DbContextEditorServices>();
            serviceProvider.AddServiceWithDependencies<IEntityFrameworkService, EntityFrameworkServices>();
        }
    }
}
