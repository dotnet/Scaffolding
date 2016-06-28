// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

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
            var testAppPath = Path.GetFullPath(Path.Combine(originalAppBase, "..","..","..","..","..","TestApps", testAppName));
#else
            var testAppPath = Path.GetFullPath(Path.Combine(originalAppBase, "..", "TestApps", testAppName));
#endif
            var testEnvironment = new TestApplicationInfo(applicationInfo, testAppPath, testAppName);
            var context = ProjectContext.CreateContextForEachFramework(testAppPath).First();
            var exporter = new LibraryExporter(context, testEnvironment);
            var workspace = new ProjectJsonWorkspace(context);
            return new WebHostBuilder()
                .UseServer(new DummyServer())
                .UseStartup<ModelTypesLocatorTestWebApp.Startup>()
                .ConfigureServices(services => 
                    {
                        services.AddSingleton<IApplicationInfo>(testEnvironment);
                        services.AddSingleton<ILibraryExporter>(exporter);
                        services.AddSingleton<CodeAnalysis.Workspace>(workspace);
                    })
                .Build()
                .Services;
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
    }

}