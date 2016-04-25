// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Xunit;

namespace ConsoleApplication
{
    public class ApplicationInfoTests
    {
        ApplicationInfo _applicationInfo;

        [Fact]
        public void ApplicationEnvironment_Test()
        {
            _applicationInfo = new ApplicationInfo("TestApplication", Directory.GetCurrentDirectory());
            Assert.Equal(Directory.GetCurrentDirectory(), _applicationInfo.ApplicationBasePath);
            Assert.Equal("TestApplication", _applicationInfo.ApplicationName);
        }
    }
}
