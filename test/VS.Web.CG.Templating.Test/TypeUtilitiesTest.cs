// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating.Test
{
    public class TypeUtilitiesTest
    {
        [Theory, MemberData(nameof(TestData_IsPrimitive))]
        public void TestIsTypePrimitive(Type type, bool expectedValue)
        {
            Assert.Equal(expectedValue, TypeUtilities.IsTypePrimitive(type));
        }

        public static IEnumerable<object[]> TestData_IsPrimitive
        {
            get
            {
                return new[]
                {
                    new object[] { typeof(int), true },
                    new object[] { typeof(bool), true },
                    new object[] { typeof(DateTime), true },
                    new object[] { typeof(string), true },
                    new object[] { typeof(decimal), true },
                    new object[] { typeof(int?), true },
                    new object[] { typeof(Guid), true },
                    new object[] { typeof(Type), false },
                    new object[] { typeof(decimal), true },
                    new object[] { typeof(double), true },
                    new object[] { typeof(UserDefinedStruct), false }
                };
            }
        }


        [Theory, MemberData(nameof(TestData_IsNullable))]
        public void TestIsNullable(Type type, bool expectedValue)
        {
            Assert.Equal(expectedValue, TypeUtilities.IsNullable(type));
        }


        public static IEnumerable<object[]> TestData_IsNullable
        {
            get
            {
                return new[]
                {
                    new object[] { typeof(int), false },
                    new object[] { typeof(bool), false },
                    new object[] { typeof(DateTime), false },
                    new object[] { typeof(string), true },
                    new object[] { typeof(decimal), false },
                    new object[] { typeof(int?), true },
                    new object[] { typeof(Guid), false },
                    new object[] { typeof(Type), true },
                    new object[] { typeof(Int32), false },
                    new object[] { typeof(UserDefinedStruct), false }
                };
            }
        }
    }

    struct UserDefinedStruct
    {
        int Value { get;set; }
    }
}