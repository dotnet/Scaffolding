// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class DefaultAssemblyLoadContextTests : TestBase
    {
        DefaultAssemblyLoadContext _defaultAssemblyLoadContext;

        public DefaultAssemblyLoadContextTests()
            : base(Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary"))
        {
        }

        //[Fact]
        public void DefaultAssemblyLoadContext_Test()
        {
            var library = new LibraryManager(_projectContext).GetLibrary("ModelTypesLocatorTestClassLibrary");
            var path = new LibraryExporter(_projectContext, _environment).GetResolvedPathForDependency(library);

            var currentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "bin", "debug", "dnxcore50");
            //var assemblyName = "Microsoft.Extensions.CodeGeneration.Sources.Test";
            _defaultAssemblyLoadContext = new DefaultAssemblyLoadContext(
                                                new Dictionary<AssemblyName, string>(),
                                                new Dictionary<string, string>(),
                                                new List<string>() { currentDirectory });
            Assembly assembly;
            assembly = _defaultAssemblyLoadContext.LoadFromPath(path);
            Assert.NotNull(assembly);
            Assert.True(assembly.DefinedTypes.Where(_ => _.Name == "ModelWithMatchingShortName").Any());
        }
    }
}
