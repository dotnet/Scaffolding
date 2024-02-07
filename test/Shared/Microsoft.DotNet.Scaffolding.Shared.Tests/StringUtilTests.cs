// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class StringUtilTests
    {
        [Fact]
        public void ContainsIgnoreCase_ShouldReturnCorrectResults()
        {
            // Arrange
            string input1 = "Hello";
            string input2 = "World";
            string input3 = "";
            string input4 = "";
            string input5 = "test";
            string input6 = "Hello123";
            string input7 = "123$%#Hello";
            string input8 = null;

            string value1 = "hello";
            string value2 = "word";
            string value3 = string.Empty;
            string value4 = "test";
            string value5 = "";
            string value6 = "hello123";
            string value7 = "123$%#hello";
            string value8 = null;

            // Act and Assert
            Assert.True(input1.ContainsIgnoreCase(value1));
            Assert.False(input2.ContainsIgnoreCase(value2));
            Assert.False(input3.ContainsIgnoreCase(value3));
            Assert.False(input4.ContainsIgnoreCase(value4));
            Assert.False(input5.ContainsIgnoreCase(value5));
            Assert.True(input6.ContainsIgnoreCase(value6));
            Assert.True(input7.ContainsIgnoreCase(value7));
            //null.ContainsIgnoreCase(string) should return false
            Assert.False(input8.ContainsIgnoreCase(value7));
            //null.ContainsIgnoreCase(null) should return false
            Assert.False(input8.ContainsIgnoreCase(value8));
            //string.ContainsIgnoreCase(null) should return false
            Assert.False(input7.ContainsIgnoreCase(value8));
        }

        [SkippableTheory]
        [MemberData(nameof(ToPathTestData))]
        public void ToPathTests(string namespaceString, string basePath, string projectRootNamespace, string expectedPath)
        {
            Skip.If(!OperatingSystem.IsWindows());
            Assert.Equal(expectedPath, StringUtil.ToPath(namespaceString, basePath, projectRootNamespace));
        }

        [SkippableTheory]
        [MemberData(nameof(ToNamespaceTestData))]
        public void ToNamespaceTests(string path, string expectedNamespace)
        {
            Skip.If(!OperatingSystem.IsWindows());
            Assert.Equal(expectedNamespace, StringUtil.ToNamespace(path));
        }

        [SkippableTheory]
        [MemberData(nameof(GetFilePathWithoutExtensionTestData))]
        public void GetFilePathWithoutExtensionTests(string fullFileName, string expected)
        {
            Skip.If(!OperatingSystem.IsWindows());
            Assert.Equal(expected, StringUtil.GetFilePathWithoutExtension(fullFileName));
        }

        public static IEnumerable<object[]> ToPathTestData
        {
            get
            {
                return new[]
                {
                    new object[] { "Project.Namespace.SubNamespace", "C:\\Some\\Path\\Project.csproj", "Project", "C:\\Some\\Path\\Project\\Namespace\\SubNamespace" },
                    new object[] { "Project.Namespace.SubNamespace", "C:\\Some\\Path\\", "Project", "C:\\Some\\Path\\Project\\Namespace\\SubNamespace" },
                    //special case : default namespace of a project in VS solution folder
                    new object[] { "Project.Components.Account", "C:\\SomePath\\Project\\Project\\", "Project", "C:\\SomePath\\Project\\Project\\Components\\Account" },
                    //special case : default namespace of a project in just a project folder
                    new object[] { "Project.Components.Account", "C:\\SomePath\\Project\\", "Project", "C:\\SomePath\\Project\\Components\\Account" },
                    new object[] { "Project.Namespace.SubNamespace", "", "Project", "" },
                    new object[] { "Project", "C:\\Some\\Path\\Project.csproj", "Project", "C:\\Some\\Path\\Project" },
                    new object[] { "Project", "", "Project", "" },
                    new object[] { "", "C:\\Some\\Path\\Project.csproj", "Project", "" },
                };
            }
        }

        public static IEnumerable<object[]> ToNamespaceTestData
        {
            get
            {
                return new[]
                {
                    new object[] { "Some\\Path\\Project\\Namespace\\SubNamespace\\file.txt", "Some.Path.Project.Namespace.SubNamespace" },
                    new object[] { "Some\\Path\\Project\\Namespace\\SubNamespace\\", "Some.Path.Project.Namespace.SubNamespace" },
                    new object[] { "Some\\Path\\Project\\Namespace\\SubNamespace", "Some.Path.Project.Namespace.SubNamespace" },
                    new object[] { "Some\\Path\\", "Some.Path" },
                    new object[] { "Namespace\\", "Namespace" },
                    new object[] { "", "" },
                    new object[] { null, "" }
                };
            }
        }

        public static IEnumerable<object[]> GetFilePathWithoutExtensionTestData
        {
            get
            {
                return new[]
                {
                    new object[] { "Some\\Path\\Project\\Namespace\\SubNamespace\\file.txt", "Some.Path.Project.Namespace.SubNamespace.file" },
                    new object[] { "Some\\Path\\Project\\Namespace\\SubNamespace\\thing.cs", "Some.Path.Project.Namespace.SubNamespace.thing" },
                    new object[] { "Some\\Path\\", "Some.Path" },
                    new object[] { "Namespace\\", "Namespace" },
                    new object[] { "", "" },
                    new object[] { null, "" }
                };
            }
        }
    }
}
