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
        DefaultAssemblyLoadContext _defaultAssemblyLoadContext;

        public DefaultAssemblyLoadContextTests()
            : base(Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary"))
        {
        }

        [Fact(Skip ="Functionality broken")]
        public void DefaultAssemblyLoadContext_Test()
        {
            var library = new LibraryManager(_projectContext).GetLibrary("ModelTypesLocatorTestClassLibrary");
            var path = new LibraryExporter(_projectContext, _applicationInfo).GetResolvedPathForDependency(library);

            //var currentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "bin", "debug", "dnxcore50");
            ////var assemblyName = "Microsoft.Extensions.CodeGeneration.Sources.Test";
            //_defaultAssemblyLoadContext = DefaultAssemblyLoadContext.CreateAssemblyLoadContext();
            //Assembly assembly;
            //assembly = _defaultAssemblyLoadContext.LoadFromPath(path);
            //Assert.NotNull(assembly);
            //Assert.True(assembly.DefinedTypes.Where(_ => _.Name == "ModelWithMatchingShortName").Any());
        }
    }
}
