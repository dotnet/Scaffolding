// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.CodeGeneration
{
    public class ActionDescriptor
    {
        private Type _actionModel;
        private List<ParameterDescriptor> _parameters;

        public ActionDescriptor([NotNull]CodeGeneratorDescriptor descriptor,
            [NotNull]MethodInfo method)
        {
            Generator = descriptor;
            ActionMethod = method;
        }

        public MethodInfo ActionMethod
        {
            get;
            private set;
        }

        public Type ActionModel
        {
            get
            {
                if (_actionModel == null)
                {
                    var parameters = ActionMethod.GetParameters();
                    Contract.Assert(parameters.Count() == 1);

                    _actionModel = parameters.First().ParameterType;
                }
                return _actionModel;
            }
        }

        public CodeGeneratorDescriptor Generator
        {
            get;
            private set;
        }

        public List<ParameterDescriptor> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = ActionModel
                        .GetRuntimeProperties()
                        .Where(pi => IsCandidateProperty(pi))
                        .Select(pi => new ParameterDescriptor(pi))
                        .ToList();
                }
                return _parameters;
            }
        }

        private bool IsCandidateProperty([NotNull]PropertyInfo property)
        {
            return property.CanWrite &&
                property.GetIndexParameters().Length == 0 &&
                IsSupportedPropertyType(property.PropertyType);
        }

        private bool IsSupportedPropertyType(Type propertyType)
        {
            return propertyType == typeof(string) ||
                propertyType == typeof(bool);
        }
    }
}