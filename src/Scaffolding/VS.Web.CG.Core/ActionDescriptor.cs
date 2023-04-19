// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ActionDescriptor
    {
        private Type _actionModel;
        private List<ParameterDescriptor> _parameters;

        public ActionDescriptor(CodeGeneratorDescriptor descriptor,
            MethodInfo method)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

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
                    Debug.Assert(parameters.Count() == 1);

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

        private bool IsCandidateProperty(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

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