// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGeneration.Core.FunctionalTest
{
    public static class TestHelper
    {
        public static IServiceProvider CreateServices(string testAppName)
        {
            var originalProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = originalProvider.GetRequiredService<IApplicationEnvironment>();

            //When the tests are run the appEnvironment points to test project.
            //Change the app environment to point to the test application to be used
            //by test.
            var originalAppBase = appEnvironment.ApplicationBasePath; ////Microsoft.Extensions.CodeGeneration.Core.FunctionalTest
            var testAppPath = Path.GetFullPath(Path.Combine(originalAppBase, "..", "TestApps", testAppName));

            return new WebHostBuilder(originalProvider)
                .UseServices(services => services.AddInstance<IApplicationEnvironment>(new TestApplicationEnvironment(appEnvironment, testAppPath, testAppName)))
                .Build()
                .ApplicationServices;
        }
    }
}