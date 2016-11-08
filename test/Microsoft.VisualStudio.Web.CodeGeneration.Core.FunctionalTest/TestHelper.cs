// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
{
    public static class TestHelper
    {
        public static IServiceProvider CreateServices(string testAppName)
        {
#if RELEASE
            var applicationInfo = new ApplicationInfo(testAppName, Directory.GetCurrentDirectory(), "Release");
#else
            var applicationInfo = new ApplicationInfo(testAppName, Directory.GetCurrentDirectory(), "Debug");
#endif
            // When the tests are run the applicationInfo points to test project.
            // Change the app applicationInfo to point to the test application to be used
            // by test.
            var originalAppBase = applicationInfo.ApplicationBasePath; ////Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
#if NET451
            var testAppPath = Path.GetFullPath(Path.Combine(originalAppBase, "..","..","..","..","..", "..","TestApps", testAppName));
#else
            var testAppPath = Path.GetFullPath(Path.Combine(originalAppBase, "..", "..", "TestApps", testAppName));
#endif
            var errors = new List<string>();
            var output = new List<string>();
            // Restore the project.
            RunDotNet("restore", Path.GetDirectoryName(testAppPath));
            RunDotNet("build", testAppPath);

            var testEnvironment = new TestApplicationInfo(applicationInfo, testAppPath, testAppName);
            var context = ProjectContext.CreateContextForEachFramework(testAppPath).First();
            var exporter = new LibraryExporter(context, testEnvironment);
            var workspace = new ProjectJsonWorkspace(context);
            return new WebHostBuilder()
                .UseServer(new DummyServer())
                .UseStartup<Startup>()
                .ConfigureServices(services => 
                    {
                        services.AddSingleton<IApplicationInfo>(testEnvironment);
                        services.AddSingleton<ILibraryExporter>(exporter);
                        services.AddSingleton<CodeAnalysis.Workspace>(workspace);
                    })
                .Build()
                .Services;
        }

        private static void RunDotNet(string commandName, string testAppPath)
        {
            var errors = new List<string>();
            var output = new List<string>();

            var result = Command
                .CreateDotNet(commandName, new string[] { testAppPath })
                .CaptureStdErr()
                .CaptureStdOut()
                .OnErrorLine(l => errors.Add(l))
                .OnOutputLine(l => output.Add(l))
                .Execute();

            if (result.ExitCode != 0)
            {
                Console.WriteLine($"Failed to run dotnet {commandName} {testAppPath}");
                Console.WriteLine(string.Join(Environment.NewLine, output));
                Console.WriteLine(string.Join(Environment.NewLine, errors));
                Assert.True(false);
            }
        }

        private class DummyServer : IServer
        {
            IFeatureCollection IServer.Features { get; }

            public void Dispose()
            {
            }

            void IServer.Start<TContext>(IHttpApplication<TContext> application)
            {
            }
        }

        private class Startup 
        {
            public Startup(IHostingEnvironment env)
            {
            }

            public void ConfigureServices(IServiceCollection services)
            {

            }
            public void Configure(AspNetCore.Builder.IApplicationBuilder app, IHostingEnvironment env, Extensions.Logging.ILoggerFactory loggerFactory)
            {
            }
        }
    }

}