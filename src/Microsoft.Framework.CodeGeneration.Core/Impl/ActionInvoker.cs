// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace Microsoft.Framework.CodeGeneration
{
    internal class ActionInvoker
    {
        private List<ParameterDescriptor> _parameters;

        public ActionInvoker([NotNull]ActionDescriptor descriptor)
        {
            ActionDescriptor = descriptor;
        }

        public ActionDescriptor ActionDescriptor
        {
            get;
            private set;
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
                    modelInstance = Activator.CreateInstance(ActionDescriptor.ActionModel);
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
                    throw new Exception("There was an error running the GenerateCode method: " + ex.Message);
                }

                return 0;
            };
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
    }
}