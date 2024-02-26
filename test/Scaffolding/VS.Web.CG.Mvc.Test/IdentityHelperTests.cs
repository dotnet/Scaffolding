// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class IdentityHelperTests
    {
        [Theory]
        [MemberData(nameof(GetClassNameFromTypeNameTestData))]
        public void GetClassNameFromTypeNameTest(string typeName, string expectedClassName)
        {
            Assert.Equal(expectedClassName, IdentityHelper.GetClassNameFromTypeName(typeName));
        }

        public static IEnumerable<object[]> GetClassNameFromTypeNameTestData
        {
            get
            {
                return new[]
                {
                    new object[] { "Project.Namespace.SubNamespace.ClassName", "ClassName"},
                    new object[] { "Project.Namespace.SubNamespace", "SubNamespace"},
                    new object[] { "Project.Namespace", "Namespace"},
                    new object[] { "Project", "Project"},
                    new object[] { "", ""},
                };
            }
        }
    }
}
