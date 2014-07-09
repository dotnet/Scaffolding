// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
                    //ToDo: Should we filter indexed parameters?
                    _parameters = ActionModel.GetTypeInfo()
                        .DeclaredProperties
                        .Where(pi => IsCandidateProperty(pi))
                        .Select(pi => new ParameterDescriptor(pi))
                        .ToList();
                }
                return _parameters;
            }
        }

        private bool IsCandidateProperty(PropertyInfo property)
        {
            return property.CanWrite && IsSupportedPropertyType(property.PropertyType);
        }

        private bool IsSupportedPropertyType(Type propertyType)
        {
            //TodO: This can be improved to support more types that can convert from string.
            //If we do that, we need to change the valueAccessor in ParameterDescriptor to
            //convert the value from command line to target type.
            return propertyType == typeof(string) ||
                propertyType == typeof(bool);
        }
    }
}