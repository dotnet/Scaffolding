// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.CodeGeneration
{
    internal class ActionDescriptor
    {
        private List<ParameterDescriptor> _parameterList;

        public ActionDescriptor([NotNull]MethodInfo method)
        {
            Method = method;
        }

        public MethodInfo Method { get; private set; }

        public List<ParameterDescriptor> Parameters
        {
            get
            {
                if (_parameterList == null)
                {
                    _parameterList = new List<ParameterDescriptor>(Method
                        .GetParameters()
                        .Select(info => new ParameterDescriptor()
                        {
                            Name = info.Name,
                            IsOptional = info.IsOptional,
                            ParameterType = info.ParameterType,
                            DefaultValue = info.DefaultValue
                        }));
                }
                return _parameterList;
            }
        }
    }
}