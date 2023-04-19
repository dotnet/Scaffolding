// Copyright (c) .NET Foundation. All rights reserved.

using System.IO;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
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
