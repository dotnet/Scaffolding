// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.Extensions.CodeGeneration.CommandLine;

namespace Microsoft.Extensions.CodeGeneration
{
    public class ParameterDescriptor
    {
        private Func<object> _valueAccessor;

        public ParameterDescriptor([NotNull]PropertyInfo property)
        {
            Property = property;
        }

        public PropertyInfo Property { get; private set; }

        public object Value
        {
            get
            {
                Contract.Assert(_valueAccessor != null, "Value was accessed too early");
                return _valueAccessor();
            }
        }

        internal void AddCommandLineParameterTo(CommandLineApplication command)
        {
            var isBoolProperty = Property.PropertyType == typeof(bool);
            var optionAttribute = Property.GetOptionAttribute();

            //Note: This means all bool properties are treated as options by default.
            //ArgumentAttribute on such a property is ignored.
            if (isBoolProperty || optionAttribute != null)
            {
                //This is just so that all the below code does not need to
                //check for null on attribute. Not pure but works.
                var nullSafeOptionAttribute = optionAttribute ?? new OptionAttribute();

                var template = GetOptionTemplate(nullSafeOptionAttribute);
                var optionType = isBoolProperty ? CommandOptionType.NoValue : CommandOptionType.SingleValue;

                var option = command.Option(template, nullSafeOptionAttribute.Description ?? "", optionType);

                _valueAccessor = () =>
                {
                    if (isBoolProperty)
                    {
                        return option.HasValue() ? true : (nullSafeOptionAttribute.DefaultValue ?? false);
                    }
                    else
                    {
                        return option.HasValue() ? option.Value() : (nullSafeOptionAttribute.DefaultValue ?? "");
                    }
                };
            }
            else
            {
                //And all other string properties are considered arguments by default
                //even if the ArgumentAttribute is not mentioned on them.
                var argumentAttribute = Property.GetArgumentAttribute();
                var description = argumentAttribute != null && !string.IsNullOrWhiteSpace(argumentAttribute.Description)
                    ? argumentAttribute.Description
                    : "";

                var argument = command.Argument(Property.Name, description);
                _valueAccessor = () => argument.Value;
            }
        }

        private string GetOptionTemplate([NotNull]OptionAttribute optionAttribute)
        {
            var longName = !string.IsNullOrWhiteSpace(optionAttribute.Name) ? optionAttribute.Name : Property.Name;
            var template = "--" + longName;

            if (!string.IsNullOrWhiteSpace(optionAttribute.ShortName))
            {
                template += "|-" + optionAttribute.ShortName;
            }

            return template;
        }
    }
}