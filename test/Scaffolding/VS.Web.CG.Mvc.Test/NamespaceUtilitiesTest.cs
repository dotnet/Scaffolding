// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Test
{
    public class NamespaceUtilitiesTest
    {
        [Theory, MemberData(nameof(NameSpaceData))]
        public void TestGetSafeNameSpaceName(string nameSpaceName, string expected)
        {
            Assert.Equal(expected, NameSpaceUtilities.GetSafeNamespaceName(nameSpaceName));
        }

        public static IEnumerable<object[]> NameSpaceData
        {
            get
            {
                return new[]
                {
                    new object[] {"Valid.NameSpace","Valid.NameSpace"},
                    new object[] {"NameSpace with spaces","NameSpace_with_spaces"},
                    new object[] {"Namespace-with-hyphens","Namespace_with_hyphens"},
                    new object[] {"9.0namespace.numb3r3d","_9._0namespace.numb3r3d"},
                    new object[] {"","_"},
                    new object[] {"    ","_"},
                    new object[] { "prénom", "prénom" }
                };
            }
        }

        [Theory, MemberData(nameof(NamespaceFromPathData))]
        public void TestGetSafeNameSpaceNameFromPath(string path, string prefix, string expected)
        {
            Assert.Equal(expected, NameSpaceUtilities.GetSafeNamespaceFromPath(path, prefix));
        }

        private static char sep = Path.DirectorySeparatorChar;

        public static IEnumerable<object[]> NamespaceFromPathData
        {
            get
            {
                return new[]
                {
                    new object[] {$"Valid{sep}NameSpace", null, "Valid.NameSpace"},
                    new object[] {$"{sep}Valid{sep}NameSpace", null, "Valid.NameSpace"},
                    new object[] {$"Valid{sep}Name.Space", null, "Valid.Name.Space"},
                    new object[] {$"Valid{sep}NameSpace", "prefix", "prefix.Valid.NameSpace"},
                    new object[] {$"{sep}Valid{sep}NameSpace", "prefix", "prefix.Valid.NameSpace"},
                    new object[] {$"Valid{sep}Name.Space", "prefix", "prefix.Valid.Name.Space"},
                    new object[] {$"..{sep}..{sep}name.space", null, "name.space"},
                    new object[] {$"..{sep}..{sep}", null, "_"}
                };
            }
        }
    }
}
