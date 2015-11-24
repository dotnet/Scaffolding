// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CodeGeneration.CommandLine;

namespace Microsoft.Extensions.CodeGeneration
{
    internal static class MemberInfoExtensions
    {
        public static AliasAttribute GetAliasAttribute(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var aliasAttributeData = member.GetAttributeData<AliasAttribute>();

            if (aliasAttributeData != null)
            {
                //Isn't there a better way to get the value of attribute?
                return new AliasAttribute((string)aliasAttributeData.ConstructorArguments[0].Value);
            }
            return null;
        }

        public static OptionAttribute GetOptionAttribute(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var optionAttributeData = member.GetAttributeData<OptionAttribute>();

            if (optionAttributeData != null)
            {
                return new OptionAttribute()
                {
                    // This is kind of fragile, refactoring a property name will break this
                    // code, is there a better way? One good thing is we have unit tests to cover this now.
                    Name = (string)optionAttributeData.GetNamedArgumentValue("Name"),
                    ShortName = (string)optionAttributeData.GetNamedArgumentValue("ShortName"),
                    DefaultValue = optionAttributeData.GetNamedArgumentValue("DefaultValue"),
                    Description = (string)optionAttributeData.GetNamedArgumentValue("Description"),
                };
            }
            return null;
        }

        public static ArgumentAttribute GetArgumentAttribute(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var argumentAttributeData = member.GetAttributeData<ArgumentAttribute>();

            if (argumentAttributeData != null)
            {
                return new ArgumentAttribute()
                {
                    Description = (string)argumentAttributeData.GetNamedArgumentValue("Description"),
                };
            }
            return null;
        }

        private static CustomAttributeData GetAttributeData<T>(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            return member.CustomAttributes
                .Where(attr => attr.AttributeType == typeof(T))
                .FirstOrDefault();
        }

        private static object GetNamedArgumentValue(this CustomAttributeData attributeData,
            string memberName)
        {
            if (attributeData == null)
            {
                throw new ArgumentNullException(nameof(attributeData));
            }

            return attributeData
                .NamedArguments
                .Where(arg => arg.MemberName == memberName)
                .FirstOrDefault() //Since CustomAttributeNamedArgument are structs, the default will not be null but the Value will be null which is what we want anyway...
                .TypedValue.Value;
        }
    }
}