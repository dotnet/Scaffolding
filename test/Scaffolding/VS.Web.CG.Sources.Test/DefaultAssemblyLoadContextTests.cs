// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
