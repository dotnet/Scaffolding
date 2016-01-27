using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CodeGeneration.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.Extensions.CodeGeneration.Templating;
using Microsoft.Extensions.CodeGeneration.Templating.Compilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.DotNet.ProjectModel;
using System.Runtime.Loader;
using System.IO;
using System.Threading;
using Microsoft.DotNet.ProjectModel.Loader;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.CodeGeneration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            PrintCommandLine(args);
            var app = new CommandLineApplication(false)
            {
                Name = "Code Generation",
                Description = "Code generation for Asp.net"
            };
            app.HelpOption("-h|--help");
            var projectPath = app.Option("-p|--project", "Path to project.json", CommandOptionType.SingleValue);
            var packagesPath = app.Option("-n|--nugetPackageDir", "Path to check for Nuget packages", CommandOptionType.SingleValue);
            
            app.OnExecute(() =>
            {
                PrintCommandLine(projectPath, app.RemainingArguments);
                var serviceProvider = new ServiceProvider();
                var context = CreateProjectContext(projectPath.Value());
                Directory.SetCurrentDirectory(context.ProjectDirectory);
                Console.WriteLine("Current Directory: " + Directory.GetCurrentDirectory());
                AddFrameworkServices(serviceProvider, context, packagesPath.Value());
                AddCodeGenerationServices(serviceProvider);
                var codeGenCommand = new CodeGenCommand(serviceProvider);
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
            Console.WriteLine("RunTime " + elapsedTime);
        }

        private static void PrintCommandLine(CommandOption projectPath, List<string> remainingArguments)
        {
            Console.WriteLine("Command line After parsing :: ");
            if(projectPath != null)
            {
                Console.WriteLine(string.Format("    Project path: {0}", projectPath.Value()));
            }
            Console.WriteLine("    Remaining Args :: ");
            if(remainingArguments != null)
            {
                foreach (var arg in remainingArguments)
                {
                    Console.WriteLine("        "+arg);
                }
            }
        }

        private static void AddFrameworkServices(ServiceProvider serviceProvider, ProjectContext context, string nugetPackageDir)
        {
            //TODO : Create  two different types of loaders. 
            // One for loading up code generation assemblies.
            // Other one for loading project itself.
            // Maybe better to have a assembly Loader and another one as project compilation workspace. 
            // Use the workspace to get the compilation of the project and model types. 
            // Use the loader to figure out which assemblies are installed by the project that can be used as code gen assemblies. 
            serviceProvider.Add(typeof(IServiceProvider), serviceProvider);
            serviceProvider.Add(typeof(ProjectContext), context);
            serviceProvider.Add(typeof(Workspace), context.CreateWorkspace());
            serviceProvider.Add(typeof(IApplicationEnvironment),new ApplicationEnvironment(context.RootProject.Identity.Name, context.ProjectDirectory));
            serviceProvider.Add(typeof(ICodeGenAssemblyLoadContext), GetAssemblyLoadContext(context.CreateLoadContext(), nugetPackageDir));
            serviceProvider.Add(typeof(AssemblyLoadContext), context.CreateLoadContext());
            serviceProvider.Add(typeof(ILibraryManager), new LibraryManager(context));
            serviceProvider.Add(typeof(ILibraryExporter), new LibraryExporter(context));
        }

        private static ICodeGenAssemblyLoadContext GetAssemblyLoadContext(AssemblyLoadContext projectLoadContext, string nugetPackageDir)
        {
            List<string> searchPaths = new List<string>();
            if(Directory.Exists(nugetPackageDir))
            {
                Queue<string> queue = new Queue<string>();
                queue.Enqueue(nugetPackageDir);
                while(queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    searchPaths.Add(current);
                    try
                    {
                        var subdirs = Directory.EnumerateDirectories(current);
                        if(subdirs != null)
                        {
                            foreach(var sd in subdirs)
                            {
                                queue.Enqueue(sd);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }
            }

            return new DefaultAssemblyLoadContext(null, null, searchPaths, projectLoadContext as ProjectLoadContext);
        }

        private static ProjectContext CreateProjectContext(string projectPath)
        {
            projectPath = projectPath ?? Directory.GetCurrentDirectory();

            if (!projectPath.EndsWith(Microsoft.DotNet.ProjectModel.Project.FileName))
            {
                projectPath = Path.Combine(projectPath, Microsoft.DotNet.ProjectModel.Project.FileName);
            }

            if (!File.Exists(projectPath))
            {
                throw new InvalidOperationException($"{projectPath} does not exist.");
            }

            return ProjectContext.CreateContextForEachFramework(projectPath).FirstOrDefault();
        }

        private static void PrintCommandLine(string []args)
        {
            Console.WriteLine("Raw command line ::");
            if(args != null)
            {
                foreach(string arg in args)
                {
                    Console.WriteLine("    "+arg);
                }
            }
            else
            {
                Console.WriteLine("No arguments!!! >-<");
            }
        }

        private static void AddCodeGenerationServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            //Ordering of services is important here
            serviceProvider.Add(typeof(ILogger), new ConsoleLogger());
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<ICodeGeneratorAssemblyProvider, DefaultCodeGeneratorAssemblyProvider>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorLocator, CodeGeneratorsLocator>();

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
