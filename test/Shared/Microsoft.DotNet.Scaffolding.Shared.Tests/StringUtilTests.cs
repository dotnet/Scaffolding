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

        [Theory]
        [MemberData(nameof(ToPathTestData))]
        public void ToPathTests(string namespaceString, string basePath, string projectRootNamespace, string expectedPath)
        {
            Assert.Equal(expectedPath, StringUtil.ToPath(namespaceString, basePath, projectRootNamespace));
        }

        [Theory]
        [MemberData(nameof(ToNamespaceTestData))]
        public void ToNamespaceTests(string path, string expectedNamespace)
        {
            Assert.Equal(expectedNamespace, StringUtil.ToNamespace(path));
        }

        [Theory]
        [MemberData(nameof(GetFilePathWithoutExtensionTestData))]
        public void GetFilePathWithoutExtensionTests(string fullFileName, string expected)
        {
            Assert.Equal(expected, StringUtil.GetFilePathWithoutExtension(fullFileName));
        }

        public static IEnumerable<object[]> ToPathTestData
        {
            get
            {
                string root1 = OperatingSystem.IsWindows() ? @"C:\Some\Path" : "/some/path";
                string root2 = OperatingSystem.IsWindows() ? @"C:\SomePath" : "/somepath";
                return
                [
                    new object[] { "Project.Namespace.SubNamespace", Path.Combine(root1, "Project.csproj"), "Project", Path.Combine(root1, "Project", "Namespace", "SubNamespace") },
                    new object[] { "Project.Web.Namespace.SubNamespace", Path.Combine(root1, "Project.Web.csproj"), "Project.Web", Path.Combine(root1, "Project.Web", "Namespace", "SubNamespace") },
                    new object[] { "Project.Namespace.SubNamespace", root1 + Path.DirectorySeparatorChar, "Project", Path.Combine(root1, "Project", "Namespace", "SubNamespace") },
                    //special case : default namespace of a project in VS solution folder
                    new object[] { "Project.Components.Account", Path.Combine(root2, "Project", "Project") + Path.DirectorySeparatorChar, "Project", Path.Combine(root2, "Project", "Project", "Components", "Account") },
                    //special case : default namespace of a project in just a project folder
                    new object[] { "Project.Components.Account", Path.Combine(root2, "Project") + Path.DirectorySeparatorChar, "Project", Path.Combine(root2, "Project", "Components", "Account") },
                    new object[] { "Project.Namespace.SubNamespace", "", "Project", "" },
                    new object[] { "Project", Path.Combine(root1, "Project.csproj"), "Project", Path.Combine(root1, "Project", "Project") },
                    new object[] { "Project", "", "Project", "" },
                    new object[] { "", Path.Combine(root1, "Project.csproj"), "Project", "" },
                ];
            }
        }

        public static IEnumerable<object[]> ToNamespaceTestData
        {
            get
            {
                char sep = Path.DirectorySeparatorChar;
                return new[]
                {
                    new object[] { Path.Combine("Some", "Path", "Project", "Namespace", "SubNamespace", "file.txt"), "Some.Path.Project.Namespace.SubNamespace" },
                    new object[] { Path.Combine("Some", "Path", "Project", "Namespace", "SubNamespace") + sep, "Some.Path.Project.Namespace.SubNamespace" },
                    new object[] { Path.Combine("Some", "Path", "Project", "Namespace", "SubNamespace"), "Some.Path.Project.Namespace.SubNamespace" },
                    new object[] { Path.Combine("Some", "Path") + sep, "Some.Path" },
                    new object[] { "Namespace" + sep, "Namespace" },
                    new object[] { "", "" },
                    new object[] { null, "" }
                };
            }
        }

        public static IEnumerable<object[]> GetFilePathWithoutExtensionTestData
        {
            get
            {
                char sep = Path.DirectorySeparatorChar;
                return new[]
                {
                    new object[] { Path.Combine("Some", "Path", "Project", "Namespace", "SubNamespace", "file.txt"), "Some.Path.Project.Namespace.SubNamespace.file" },
                    new object[] { Path.Combine("Some", "Path", "Project", "Namespace", "SubNamespace", "thing.cs"), "Some.Path.Project.Namespace.SubNamespace.thing" },
                    new object[] { Path.Combine("Some", "Path") + sep, "Some.Path" },
                    new object[] { "Namespace" + sep, "Namespace" },
                    new object[] { "", "" },
                    new object[] { null, "" }
                };
            }
        }
    }
}
