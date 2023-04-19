// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class CompilationResultTests
    {
        [Fact]
        public void CompilationResult_TestFromAssembly()
        {

            Assembly assembly = Assembly.GetEntryAssembly();

            var result = CompilationResult.FromAssembly(assembly);

            Assert.True(result.Success);
        }

        [Fact]
        public void CompilationResult_TestFromErrorMessage()
        {
            var result = CompilationResult.FromErrorMessages(new List<string>() { "error1", "error2", "error3" });

            Assert.False(result.Success);
            Assert.Contains("error1", result.ErrorMessages);
            Assert.Equal(3, result.ErrorMessages.Count());
            Assert.Null(result.Assembly);
        }
    }
}
