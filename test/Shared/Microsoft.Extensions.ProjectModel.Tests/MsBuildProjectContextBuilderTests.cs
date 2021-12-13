using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.Extensions.ProjectModel;
using Xunit;

namespace Microsoft.Extensions.ProjectModel.Tests
{
    public class MsBuildProjectContextBuilderTests
    {
        [Fact]
        public async Task ProjectArgumentExceptionTest()
        {
            // Act
            var ex = Assert.Throws<ArgumentException>(() => new MsBuildProjectContextBuilder(String.Empty, "build"));

            // Assert
            Assert.Equal(MessageStrings.ProjectPathNotGiven, ex.Message);
        }

        [Fact]
        public async Task ProjectArgumentExceptionTest()
        {
            // Act
            var ex = Assert.Throws<ArgumentException>(() => new MsBuildProjectContextBuilder("project", String.Empty));

            // Assert
            Assert.Equal(MessageStrings.TargetLocationNotGiven, ex.Message);
        }
   }
}
