// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace Microsoft.Framework.CodeGeneration
{
    public class CodeGeneratorDescriptor
    {
        private TypeInfo _codeGeneratorType;
        private ITypeActivator _typeActivator;
        private IServiceProvider _serviceProvider;
        private ActionInvoker _codeGeneratorAction;

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
        internal string Name
        {
            get
            {
                var alias = _codeGeneratorType.GetAliasAttribute();
                return alias != null ? alias.Alias : _codeGeneratorType.Name;
            }
        }

        internal object CreateCodeGeneratorInstace()
        {
            return _typeActivator.CreateInstance(_serviceProvider, _codeGeneratorType.AsType());
        }

        private ActionInvoker CodeGeneratorAction
        {
            get
            {
                if (_codeGeneratorAction == null)
                {
                    var candidates = _codeGeneratorType
                        .GetDeclaredMethods("GenerateCode")
                        .Where(mi => IsValidAction(mi));

                    // ToDo: Throwing here means one bad code generator could make others
                    // unusable. Should change that.
                    var count = candidates.Count();
                    if (count == 0)
                    {
                        throw new Exception("GenerateCode method with a model parameter is not found in class: " + _codeGeneratorType.FullName);
                    }
                    if (count > 1)
                    {
                        throw new Exception("Multiple GenerateCode methods with a model parameter are found in class: " + _codeGeneratorType.FullName);
                    }

                    _codeGeneratorAction = new ActionInvoker(this, candidates.First());
                }
                return _codeGeneratorAction;
            }
        }

        public void Execute(string[] args)
        {
            var app = new CommandLineApplication();

            app.Command(Name, c =>
            {
                c.HelpOption("-h|-?|--help");
                CodeGeneratorAction.BuildCommandLine(c);
            });

            app.Execute(args);
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