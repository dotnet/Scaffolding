// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.Core;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class CodeGeneratorDescriptor
    {
        private const string GenerateCodeMethodName = "GenerateCode";
        private readonly TypeInfo _codeGeneratorType;
        private readonly IServiceProvider _serviceProvider;
        private ActionDescriptor _codeGeneratorAction;

        public CodeGeneratorDescriptor(TypeInfo codeGeneratorType,
            IServiceProvider serviceProvider)
        {
            if (codeGeneratorType == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorType));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

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
                        .GetDeclaredMethods(GenerateCodeMethodName)
                        .Where(mi => IsValidAction(mi));

                    var count = candidates.Count();
                    if (count == 0)
                    {
                        throw new InvalidOperationException(string.Format(MessageStrings.MethodNotFound, GenerateCodeMethodName, _codeGeneratorType.FullName));
                    }
                    if (count > 1)
                    {
                        throw new InvalidOperationException(string.Format(MessageStrings.MultipleMethodsFound, GenerateCodeMethodName, _codeGeneratorType.FullName));
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
                    throw new InvalidOperationException(string.Format(MessageStrings.CodeGeneratorInstanceCreationError, _codeGeneratorType.FullName, ex.Message));
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