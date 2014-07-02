// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGeneration
{
    //Internal?
    public class CodeGeneratorInvoker
    {
        private CodeGeneratorFactory _factory;
        private IServiceProvider _serviceProvider;
        private ITypeActivator _typeActivator;

        public CodeGeneratorInvoker(
            [NotNull]CodeGeneratorFactory factory,
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider)
        {
            _factory = factory;
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
        }

        public void Invoke(IDictionary<string, object> values)
        {
            var action = FindActionToInvoke();

            var instance = _typeActivator.CreateInstance(_serviceProvider,
                _factory.CodeGeneratorMetadata.Type);

            var parameterArgs = new List<object>();
            foreach (var param in action.Parameters)
            {
                if (values.ContainsKey(param.Name))
                {
                    parameterArgs.Add(values[param.Name]);
                }
                else if (param.IsOptional)
                {
                    parameterArgs.Add(param.DefaultValue);
                }
                else
                {
                    throw new Exception("Did not get the correct values to invoke the method: " +action.Method.Name);
                }
            }

            try
            {
                action.Method.Invoke(instance, parameterArgs.ToArray());
            }
            catch (ArgumentException)
            {
                //Todo: Logger?
                throw new Exception("The parameter types and values did not match, all switch parameters in GenerateCode must be of type boolean and all other parameters must be strings");
            }
            catch (TargetInvocationException ex)
            {
                throw new Exception("The GenerateCode method threw an exception: " + ex.Message);
            }
        }

        private ActionDescriptor FindActionToInvoke()
        {
            //Todo: Perhaps I should not do all of this?
            var reflectionType = _factory.CodeGeneratorMetadata.Type;
#if K10
            var typeInfo = reflectionType.GetTypeInfo();
#else
            var typeInfo = reflectionType.Assembly
                .DefinedTypes.Where(info => info.AsType() == reflectionType)
                .FirstOrDefault();
#endif

            Contract.Assert(typeInfo != null, "There's something wrong with the logic of getting type info from type");

            var candidateActions = typeInfo
                .GetDeclaredMethods("GenerateCode")
                .Where(mi => !mi.ContainsGenericParameters)
                .Select(mi => new ActionDescriptor(mi))
                .Where(IsValidAction);

            var count = candidateActions.Count();

            if (count == 0)
            {
                throw new Exception("There are no matching GenerateCode methods");
            }

            if (count > 1)
            {
                throw new Exception("There are multiple matching GenerateCode methods");
            }

            return candidateActions.First();
        }

        /// <summary>
        /// Currently this method makes simple assumptions that
        /// we want to invoke a method which matches the command arguments
        /// and options model, we could perhaps improve this taking into
        /// account multiple overloads??
        /// </summary>
        private bool IsValidAction([NotNull]ActionDescriptor action)
        {
            //Todo: parameter type validation??
            Func<string, bool> hasMatchingParameter = (name) => (
                action.Parameters.Any(param => string.Equals(name, param.Name, StringComparison.Ordinal))
            );

            var isMissingArgs = _factory
                .CodeGeneratorMetadata
                .Arguments
                .Any(arg => !hasMatchingParameter(arg.Name));

            var isMissingOptions = _factory
                .CodeGeneratorMetadata
                .Options
                .Any(option => !hasMatchingParameter(option.LongName));

            return !isMissingArgs && !isMissingOptions;
        }
    }
}