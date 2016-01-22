// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.Extensions.CodeGeneration.Core.FunctionalTest
{
    public static class TestHelper
    {
        public static IServiceProvider CreateServices(string testAppName)
        {
            var appEnvironment = PlatformServices.Default.Application;

            // When the tests are run the appEnvironment points to test project.
            // Change the app environment to point to the test application to be used
            // by test.
            var originalAppBase = appEnvironment.ApplicationBasePath; ////Microsoft.Extensions.CodeGeneration.Core.FunctionalTest
            var testAppPath = Path.GetFullPath(Path.Combine(originalAppBase, "..", "TestApps", testAppName));
            var testEnvironment = new TestApplicationEnvironment(appEnvironment, testAppPath, testAppName);

            return new WebHostBuilder()
                .UseServer(new DummyServer())
                .UseStartup<ModelTypesLocatorTestWebApp.Startup>()
                .ConfigureServices(services => 
                    {
                        services.AddSingleton<IApplicationEnvironment>(testEnvironment);
                        services.AddSingleton(CompilationServices.Default.LibraryExporter);
                        services.AddSingleton(CompilationServices.Default.CompilerOptionsProvider);
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