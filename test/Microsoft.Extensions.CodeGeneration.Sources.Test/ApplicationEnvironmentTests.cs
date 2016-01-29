// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;
using Microsoft.Extensions.CodeGeneration.DotNet;
using System.IO;

namespace ConsoleApplication
{
    public class ApplicationEnvironmentTests
    {
        ApplicationEnvironment _applicationEnvironment;

        [Fact]
        public void ApplicationEnvironment_Test()
        {
            _applicationEnvironment = new ApplicationEnvironment("TestApplication", Directory.GetCurrentDirectory());
            Assert.Equal(Directory.GetCurrentDirectory(), _applicationEnvironment.ApplicationBasePath);
            Assert.Equal("TestApplication", _applicationEnvironment.ApplicationName);
        }
    }
}
