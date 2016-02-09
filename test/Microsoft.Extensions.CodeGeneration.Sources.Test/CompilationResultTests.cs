// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class CompilationResultTests
    {
        [Fact]
        public void CompilationResult_TestFromAssembly()
        {
            Assembly assembly = new Mock<Assembly>().Object;

            var result =  CompilationResult.FromAssembly(assembly);

            Assert.True(result.Success);
        }

        [Fact]
        public void CompilationResult_TestFromErrorMessage()
        {
            var result = CompilationResult.FromErrorMessages(new List<string>() { "error1", "error2", "error3" });

            Assert.False(result.Success);
            Assert.True(result.ErrorMessages.Contains("error1"));
            Assert.Equal(3, result.ErrorMessages.Count());
            Assert.Null(result.Assembly);
        }
    }
}
