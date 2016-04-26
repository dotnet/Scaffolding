// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class DefaultAssemblyLoadContextTests : TestBase
    {
#if NET451
        static string testAppPath = Path.Combine("..", "..", "..", "..");
#else
        static string testAppPath = Directory.GetCurrentDirectory();
#endif
        DefaultAssemblyLoadContext _defaultAssemblyLoadContext;

        public DefaultAssemblyLoadContextTests()
            : base(testAppPath)
        {
        }

        [Fact]
        public void DefaultAssemblyLoadContext_Test()
        {
            var assemblyName = "Microsoft.Extensions.CodeGeneration.Sources.Test";
            _defaultAssemblyLoadContext = new DefaultAssemblyLoadContext();
            Assembly assembly = _defaultAssemblyLoadContext.LoadFromName(new AssemblyName(assemblyName));
            Assert.NotNull(assembly);
            Assert.True(assembly.DefinedTypes.Where(_ => _.Name == "DefaultAssemblyLoadContextTests").Any());
        }
    }
}
