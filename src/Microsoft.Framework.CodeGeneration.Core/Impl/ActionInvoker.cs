// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace Microsoft.Framework.CodeGeneration
{
    internal class ActionInvoker
    {
        private Type _actionModel;
        private List<ParameterDescriptor> _parameters;
        private CodeGeneratorDescriptor _descriptor;
        private MethodInfo _method;

        public ActionInvoker([NotNull]CodeGeneratorDescriptor descriptor,
            [NotNull]MethodInfo method)
        {
            _method = method;
            _descriptor = descriptor;
        }

        public Type ActionModel
        {
            get
            {
                if (_actionModel == null)
                {
                    var parameters = _method.GetParameters();
                    Contract.Assert(parameters.Count() == 1);

                    _actionModel = parameters.First().ParameterType;
                }
                return _actionModel;
            }
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

        internal void BuildCommandLine(CommandLineApplication command)
        {
            foreach (var param in Parameters)
            {
                param.AddCommandLineParameterTo(command);
            }

            command.Invoke = () =>
            {
                object modelInstance;
                try
                {
                    modelInstance = Activator.CreateInstance(ActionModel);
                }
                catch (Exception ex)
                {
                    throw new Exception("There was an error attempting to create an instace of model for GenerateCode method: " + ex.Message);
                }

                foreach (var param in Parameters)
                {
                    param.Property.SetValue(modelInstance, param.Value);
                }

                var codeGeneratorInstance = _descriptor.CreateCodeGeneratorInstace();
                _method.Invoke(codeGeneratorInstance, new[] { modelInstance });

                return 0;
            };
        }
    }
}