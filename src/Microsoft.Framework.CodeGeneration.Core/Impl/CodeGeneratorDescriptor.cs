// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGeneration
{
    public class CodeGeneratorDescriptor : ICodeGeneratorDescriptor
    {
        private TypeInfo _codeGeneratorType;
        private ITypeActivator _typeActivator;
        private IServiceProvider _serviceProvider;
        private ActionDescriptor _codeGeneratorAction;

        public CodeGeneratorDescriptor([NotNull]TypeInfo codeGeneratorType,
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider)
        {
            _codeGeneratorType = codeGeneratorType;
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
        }

        //Todo: Right now this assumes we allow invocation only on Alias if one exists
        //and only fallback to type name otherwise. Perhaps we may need to support both?
        public string Name
        {
            get
            {
                var alias = _codeGeneratorType.GetAliasAttribute();
                return alias != null ? alias.Alias : _codeGeneratorType.Name;
            }
        }

        public IActionDescriptor CodeGeneratorAction
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
                        throw new Exception("GenerateCode method with a model parameter is not found in class: " + _codeGeneratorType.FullName);
                    }
                    if (count > 1)
                    {
                        throw new Exception("Multiple GenerateCode methods with a model parameter are found in class: " + _codeGeneratorType.FullName);
                    }

                    _codeGeneratorAction = new ActionDescriptor(this, candidates.First());
                }
                return _codeGeneratorAction;
            }
        }

        public object CodeGeneratorInstance
        {
            get
            {
                object instance;
                try
                {
                    instance = _typeActivator.CreateInstance(_serviceProvider, _codeGeneratorType.AsType());
                }
                catch (Exception ex)
                {
                    throw new Exception("There was an error creating the code generator instance: " + _codeGeneratorType.FullName + "\r\n" + ex.Message);
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