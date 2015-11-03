// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Compilation;

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
                .UseServices(services => 
                    {
                        services.AddInstance<IApplicationEnvironment>(testEnvironment);
                        services.AddInstance(CompilationServices.Default.LibraryExporter);
                        services.AddInstance(CompilationServices.Default.CompilerOptionsProvider);
                    })
                .Build()
                .ApplicationServices;
        }
    }
}