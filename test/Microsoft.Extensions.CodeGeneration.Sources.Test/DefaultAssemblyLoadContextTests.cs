// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CodeGeneration.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Xunit;
using System.IO;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class DefaultAssemblyLoadContextTests : TestBase
    {
        DefaultAssemblyLoadContext _defaultAssemblyLoadContext;

        public DefaultAssemblyLoadContextTests() : base(@"..\TestApps\ModelTypesLocatorTestClassLibrary")
        {

        }

        //[Fact]
        public void DefaultAssemblyLoadContext_Test()
        {
            var library = new LibraryManager(_projectContext).GetLibrary("ModelTypesLocatorTestClassLibrary");
            var path = new LibraryExporter(_projectContext).GetResolvedPathForDependency(library);

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
