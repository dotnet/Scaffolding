// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ParameterDescriptor
    {
        private Func<object> _valueAccessor;

        public ParameterDescriptor(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Property = property;
        }

        public PropertyInfo Property { get; private set; }

        public object Value
        {
            get
            {
                Debug.Assert(_valueAccessor != null, "Value was accessed too early");
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

        private string GetOptionTemplate(OptionAttribute optionAttribute)
        {
            if (optionAttribute == null)
            {
                throw new ArgumentNullException(nameof(optionAttribute));
            }

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
