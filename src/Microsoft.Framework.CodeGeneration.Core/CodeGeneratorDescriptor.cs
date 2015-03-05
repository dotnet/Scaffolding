// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGeneration
{
    public class CodeGeneratorDescriptor
    {
        private readonly TypeInfo _codeGeneratorType;
        private readonly IServiceProvider _serviceProvider;
        private ActionDescriptor _codeGeneratorAction;

        public CodeGeneratorDescriptor([NotNull]TypeInfo codeGeneratorType,
            [NotNull]IServiceProvider serviceProvider)
        {
            _codeGeneratorType = codeGeneratorType;
            _serviceProvider = serviceProvider;
        }

        public virtual string Name
        {
            get
            {
                var alias = _codeGeneratorType.GetAliasAttribute();
                return alias != null ? alias.Alias : _codeGeneratorType.Name;
            }
        }

        public virtual ActionDescriptor CodeGeneratorAction
        {
            get
            {
                if (_codeGeneratorAction == null)
                {
                    var candidates = _codeGeneratorType
                        .GetDeclaredMethods("GenerateCode")
                        .Where(mi => IsValidAction(mi));

                    var count = candidates.Count();
                    if (count == 0)
                    {
                        throw new InvalidOperationException("GenerateCode method with a model parameter is not found in class: " + _codeGeneratorType.FullName);
                    }
                    if (count > 1)
                    {
                        throw new InvalidOperationException("Multiple GenerateCode methods with a model parameter are found in class: " + _codeGeneratorType.FullName);
                    }

                    _codeGeneratorAction = new ActionDescriptor(this, candidates.First());
                }
                return _codeGeneratorAction;
            }
        }

        public virtual object CodeGeneratorInstance
        {
            get
            {
                object instance;
                try
                {
                    instance = ActivatorUtilities.CreateInstance(_serviceProvider, _codeGeneratorType.AsType());
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("There was an error creating the code generator instance: " + _codeGeneratorType.FullName + "\r\n" + ex.Message);
                }

                return instance;
            }
        }

        private bool IsValidAction(MethodInfo method)
        {
            if (method.ContainsGenericParameters || method.IsAbstract || method.IsStatic)
            {
                return false;
            }

            var parameters = method.GetParameters();

            if (parameters.Count() != 1)
            {
                return false;
            }

            //Should we validate the type of parameter?
            return true;
        }
    }
}