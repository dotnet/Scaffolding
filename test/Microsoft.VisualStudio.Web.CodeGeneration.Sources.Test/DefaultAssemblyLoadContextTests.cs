// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    [Collection("CodeGeneration.Utils")]
    public class DefaultAssemblyLoadContextTests //: TestBase
    {
        DefaultAssemblyLoadContext _defaultAssemblyLoadContext;

        public DefaultAssemblyLoadContextTests()
            //: base(testFixture)
        {
        }

        [Fact]
        public void DefaultAssemblyLoadContext_Test()
        {
            var assemblyName = "Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test";
            _defaultAssemblyLoadContext = new DefaultAssemblyLoadContext();
            Assembly assembly = _defaultAssemblyLoadContext.LoadFromName(new AssemblyName(assemblyName));
            Assert.NotNull(assembly);
            Assert.True(assembly.DefinedTypes.Where(_ => _.Name == "DefaultAssemblyLoadContextTests").Any());
        }
    }
}
