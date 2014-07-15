// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace Microsoft.Framework.CodeGeneration
{
    public class ActionInvoker
    {
        private List<ParameterDescriptor> _parameters;
        private readonly ITypeActivator _typeActivator;
        private readonly IServiceProvider _serviceProvider;

        public ActionInvoker([NotNull]ActionDescriptor descriptor,
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider)
        {
            ActionDescriptor = descriptor;
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
        }

        public ActionDescriptor ActionDescriptor
        {
            get;
            private set;
        }

        public void Execute(string[] args)
        {
            var app = new CommandLineApplication();

            app.Command(ActionDescriptor.Generator.Name, c =>
            {
                c.HelpOption("-h|-?|--help");
                BuildCommandLine(c);
            });

            app.Execute(args);
        }

        internal void BuildCommandLine(CommandLineApplication command)
        {
            foreach (var param in ActionDescriptor.Parameters)
            {
                param.AddCommandLineParameterTo(command);
            }

            command.Invoke = () =>
            {
                object modelInstance;
                try
                {
                    modelInstance = _typeActivator.CreateInstance(_serviceProvider, ActionDescriptor.ActionModel);
                }
                catch (Exception ex)
                {
                    throw new Exception("There was an error attempting to create an instace of model for GenerateCode method: " + ex.Message);
                }

                foreach (var param in ActionDescriptor.Parameters)
                {
                    param.Property.SetValue(modelInstance, param.Value);
                }

                var codeGeneratorInstance = ActionDescriptor.Generator.CodeGeneratorInstance;

                try
                {
                    ActionDescriptor.ActionMethod.Invoke(codeGeneratorInstance, new[] { modelInstance });
                }
                catch (TargetInvocationException ex)
                {
                    throw new Exception("There was an error running the GenerateCode method: " + ex.InnerException.Message);
                }

                return 0;
            };
        }
    }
}