// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class StringUtilTests
    {
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
